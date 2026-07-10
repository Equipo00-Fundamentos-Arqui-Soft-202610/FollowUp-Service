using System.Security.Claims;
using System.Text;
using MediTrack.FollowUpService.API.Domain.Model.Aggregates;
using MediTrack.FollowUpService.API.Domain.Model.ValueObjects;
using MediTrack.FollowUpService.API.Infrastructure.Persistence.EFC;
using MediTrack.FollowUpService.API.Infrastructure.Persistence.EFC.Configuration;
using MediTrack.FollowUpService.API.Interfaces.REST.Controllers;
using MediTrack.FollowUpService.API.Interfaces.REST.Resources;
using MediTrack.FollowUpService.Tests.TestSupport;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace MediTrack.FollowUpService.Tests;

public class ComplianceVideoControllerTests
{
    private static ComplianceVideoController BuildController(
        FollowUpDbContext context,
        FakeTemporaryVideoStorage storage,
        int patientId = 1)
    {
        var repository = new MedicationComplianceRepository(context);
        var controller = new ComplianceVideoController(
            context,
            repository,
            storage,
            NullLogger<ComplianceVideoController>.Instance);

        var claims = new[] { new Claim("patientId", patientId.ToString()), new Claim("sub", patientId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        return controller;
    }

    private static IFormFile BuildFormFile(string contentType, long length, string content = "video-bytes")
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var mock = new Mock<IFormFile>();
        mock.Setup(f => f.ContentType).Returns(contentType);
        mock.Setup(f => f.Length).Returns(length > 0 ? length : bytes.Length);
        mock.Setup(f => f.FileName).Returns("evidencia.mp4");
        mock.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(bytes));
        return mock.Object;
    }

    [Fact]
    public async Task SubmitVideo_ConVideoValido_CreaComplianceEnPendingValidation()
    {
        await using var context = TestFixtures.NewInMemoryContext();
        var doseSchedule = TestFixtures.SeedActiveDoseSchedule(context, patientId: 1, doseScheduleId: 200);
        var storage = new FakeTemporaryVideoStorage();
        var controller = BuildController(context, storage, patientId: 1);

        var result = await controller.SubmitVideo(doseSchedule.Id, BuildFormFile("video/mp4", 1024));

        var created = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        var body = Assert.IsType<VideoSubmissionResource>(created.Value);
        Assert.Equal("pendingvalidation", body.Status);

        var stored = await context.MedicationCompliances.FindAsync(body.ComplianceId);
        Assert.NotNull(stored);
        Assert.True(stored!.Status.IsPendingValidation);
        Assert.NotNull(stored.TemporaryVideoPath);
        Assert.True(storage.Exists(stored.TemporaryVideoPath!));
    }

    [Fact]
    public async Task SubmitVideo_ConContentTypeInvalido_Devuelve400()
    {
        await using var context = TestFixtures.NewInMemoryContext();
        var doseSchedule = TestFixtures.SeedActiveDoseSchedule(context);
        var controller = BuildController(context, new FakeTemporaryVideoStorage());

        var result = await controller.SubmitVideo(doseSchedule.Id, BuildFormFile("image/png", 1024));

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }

    [Fact]
    public async Task SubmitVideo_ConArchivoDemasiadoGrande_Devuelve400()
    {
        await using var context = TestFixtures.NewInMemoryContext();
        var doseSchedule = TestFixtures.SeedActiveDoseSchedule(context);
        var controller = BuildController(context, new FakeTemporaryVideoStorage());

        var oversized = BuildFormFile("video/mp4", 60L * 1024 * 1024); // 60MB > límite de 50MB

        var result = await controller.SubmitVideo(doseSchedule.Id, oversized);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetStatus_DevuelvePendingValidation_TrasSubirVideo()
    {
        await using var context = TestFixtures.NewInMemoryContext();
        var doseSchedule = TestFixtures.SeedActiveDoseSchedule(context);
        var controller = BuildController(context, new FakeTemporaryVideoStorage());

        var submitResult = await controller.SubmitVideo(doseSchedule.Id, BuildFormFile("video/mp4", 1024));
        var complianceId = ((VideoSubmissionResource)((ObjectResult)submitResult.Result!).Value!).ComplianceId;

        var statusResult = await controller.GetStatus(complianceId);

        var ok = Assert.IsType<OkObjectResult>(statusResult.Result);
        var status = Assert.IsType<ComplianceValidationStatusResource>(ok.Value);
        Assert.Equal("pendingvalidation", status.Status);
    }

    [Fact]
    public async Task Approve_MarcaComoApproved_YBorraElArchivo()
    {
        await using var context = TestFixtures.NewInMemoryContext();
        var doseSchedule = TestFixtures.SeedActiveDoseSchedule(context);
        var storage = new FakeTemporaryVideoStorage();
        var controller = BuildController(context, storage);

        var submitResult = await controller.SubmitVideo(doseSchedule.Id, BuildFormFile("video/mp4", 1024));
        var complianceId = ((VideoSubmissionResource)((ObjectResult)submitResult.Result!).Value!).ComplianceId;
        var videoFileName = (await context.MedicationCompliances.FindAsync(complianceId))!.TemporaryVideoPath!;

        var approveResult = await controller.Approve(complianceId);

        var ok = Assert.IsType<OkObjectResult>(approveResult.Result);
        var status = Assert.IsType<ComplianceValidationStatusResource>(ok.Value);
        Assert.Equal("approved", status.Status);
        Assert.NotNull(status.ValidatedAt);

        var updated = await context.MedicationCompliances.FindAsync(complianceId);
        Assert.True(updated!.Status.IsApproved);
        Assert.True(updated.Status.IsTaken); // cuenta para adherencia
        Assert.Null(updated.TemporaryVideoPath);

        // Cubre: "eliminación del archivo después de validar"
        Assert.Contains(videoFileName, storage.DeletedFileNames);
        Assert.False(storage.Exists(videoFileName));
    }

    [Fact]
    public async Task Reject_MarcaComoRejected_ConMotivo_YBorraElArchivo()
    {
        await using var context = TestFixtures.NewInMemoryContext();
        var doseSchedule = TestFixtures.SeedActiveDoseSchedule(context);
        var storage = new FakeTemporaryVideoStorage();
        var controller = BuildController(context, storage);

        var submitResult = await controller.SubmitVideo(doseSchedule.Id, BuildFormFile("video/mp4", 1024));
        var complianceId = ((VideoSubmissionResource)((ObjectResult)submitResult.Result!).Value!).ComplianceId;
        var videoFileName = (await context.MedicationCompliances.FindAsync(complianceId))!.TemporaryVideoPath!;

        var rejectResult = await controller.Reject(complianceId, new RejectComplianceResource("Video borroso"));

        var ok = Assert.IsType<OkObjectResult>(rejectResult.Result);
        var status = Assert.IsType<ComplianceValidationStatusResource>(ok.Value);
        Assert.Equal("rejected", status.Status);
        Assert.Equal("Video borroso", status.RejectionReason);

        var updated = await context.MedicationCompliances.FindAsync(complianceId);
        Assert.True(updated!.Status.IsRejected);
        Assert.False(updated.Status.IsTaken); // no cuenta para adherencia
        Assert.Null(updated.TemporaryVideoPath);
        Assert.False(storage.Exists(videoFileName));
    }

    [Fact]
    public async Task Approve_SiNoEstaPendiente_Devuelve400()
    {
        await using var context = TestFixtures.NewInMemoryContext();
        var doseSchedule = TestFixtures.SeedActiveDoseSchedule(context);
        var storage = new FakeTemporaryVideoStorage();
        var controller = BuildController(context, storage);

        var submitResult = await controller.SubmitVideo(doseSchedule.Id, BuildFormFile("video/mp4", 1024));
        var complianceId = ((VideoSubmissionResource)((ObjectResult)submitResult.Result!).Value!).ComplianceId;
        await controller.Approve(complianceId);

        var secondApprove = await controller.Approve(complianceId);

        Assert.IsType<BadRequestObjectResult>(secondApprove.Result);
    }

    [Fact]
    public async Task SubmitVideo_ReintentoTrasRechazo_ActualizaElMismoRegistro_NoDuplica()
    {
        await using var context = TestFixtures.NewInMemoryContext();
        var doseSchedule = TestFixtures.SeedActiveDoseSchedule(context);
        var storage = new FakeTemporaryVideoStorage();
        var controller = BuildController(context, storage);

        var firstSubmit = await controller.SubmitVideo(doseSchedule.Id, BuildFormFile("video/mp4", 1024));
        var firstId = ((VideoSubmissionResource)((ObjectResult)firstSubmit.Result!).Value!).ComplianceId;
        await controller.Reject(firstId, new RejectComplianceResource("no se ve bien"));

        var secondSubmit = await controller.SubmitVideo(doseSchedule.Id, BuildFormFile("video/mp4", 2048));
        var secondId = ((VideoSubmissionResource)((ObjectResult)secondSubmit.Result!).Value!).ComplianceId;

        Assert.Equal(firstId, secondId); // mismo registro, no una fila nueva
        Assert.Single(context.MedicationCompliances);

        var reloaded = await context.MedicationCompliances.FindAsync(secondId);
        Assert.True(reloaded!.Status.IsPendingValidation);
        Assert.Null(reloaded.RejectionReason);
    }
}
