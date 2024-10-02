using Microsoft.AspNetCore.Mvc;

namespace Asilifelis.Controllers.Server;

[ApiController]
[Route("/api")]
[Produces("application/ld+json", "application/activity+json")]
public class InstanceGlobalController : ControllerBase {
	[HttpPost("inbox")]
	[Produces("application/ld+json; profile=\"https://www.w3.org/ns/activitystreams\"", "application/activity+json")]
	public async Task<IActionResult> PostSharedInbox() {
		return StatusCode(StatusCodes.Status406NotAcceptable);
	}
}