using Microsoft.AspNetCore.Mvc;

namespace ChatServer.Controllers;

[ApiController]
[Route("api/movies")]
public sealed class MoviesController : ControllerBase
{
    private readonly MovieCatalogService _movies;

    public MoviesController(MovieCatalogService movies) => _movies = movies;

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        try
        {
            var movies = await _movies.GetMoviesAsync();
            return Ok(movies);
        }
        catch (FileNotFoundException ex)
        {
            return StatusCode(503, new { error = ex.Message });
        }
    }
}