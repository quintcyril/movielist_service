using ASI.Basecode.Services.Interfaces;
using ASI.Basecode.Services.ServiceModels;
using ASI.Basecode.WebApp.Mvc;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ASI.Basecode.WebApp.Controllers
{
    /// <summary>
    /// Sample MVC API slice: React (View) -> MovieController (Controller) -> MovieService -> MovieRepository -> MySQL (Model).
    /// </summary>
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class MovieController : ControllerBase<MovieController>
    {
        private readonly IMovieService _movieService;

        public MovieController(
                            IHttpContextAccessor httpContextAccessor,
                            ILoggerFactory loggerFactory,
                            IConfiguration configuration,
                            IMapper mapper,
                            IMovieService movieService) : base(httpContextAccessor, loggerFactory, configuration, mapper)
        {
            this._movieService = movieService;
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(this._movieService.GetMovies());
        }

        [HttpGet("{id:int}")]
        public IActionResult Get(int id)
        {
            var movie = this._movieService.GetMovie(id);
            return movie == null ? NotFound() : Ok(movie);
        }

        [HttpPost]
        public IActionResult Create([FromBody] MovieViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var created = this._movieService.AddMovie(model);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }

        [HttpPut("{id:int}")]
        public IActionResult Update(int id, [FromBody] MovieViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            model.Id = id;
            return this._movieService.UpdateMovie(model) ? Ok(model) : NotFound();
        }

        [HttpDelete("{id:int}")]
        public IActionResult Delete(int id)
        {
            return this._movieService.DeleteMovie(id) ? NoContent() : NotFound();
        }
    }
}
