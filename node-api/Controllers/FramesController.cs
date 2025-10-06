using Microsoft.AspNetCore.Mvc;
using node_api.Models;

namespace node_api.Controllers;

[ApiController]
[Route("[controller]")]
public class FramesController(FramesRepo repo) : ControllerBase
{
    [HttpGet]
    public IEnumerable<XrouterUdpJsonFrame> Get()
    {
        return repo.Frames;
    }
}