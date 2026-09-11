using ASI.Basecode.Data.Interfaces;
using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.Interfaces;
using ASI.Basecode.Services.ServiceModels;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ASI.Basecode.Services.Services
{
    public class MovieService : IMovieService
    {
        private readonly IMovieRepository _repository;
        private readonly IMapper _mapper;

        public MovieService(IMovieRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public List<MovieViewModel> GetMovies()
        {
            return _repository.GetMovies()
                              .OrderBy(m => m.Title)
                              .Select(m => new MovieViewModel
                              {
                                  Id = m.Id,
                                  Title = m.Title,
                                  Genre = m.Genre,
                                  ReleaseYear = m.ReleaseYear,
                                  Watched = m.Watched
                              })
                              .ToList();
        }

        public MovieViewModel GetMovie(int id)
        {
            var movie = _repository.GetMovie(id);
            return movie == null ? null : _mapper.Map<MovieViewModel>(movie);
        }

        public MovieViewModel AddMovie(MovieViewModel model)
        {
            var movie = _mapper.Map<Movie>(model);
            movie.Id = 0;
            movie.CreatedTime = DateTime.UtcNow;
            movie.UpdatedTime = DateTime.UtcNow;

            _repository.AddMovie(movie);

            return _mapper.Map<MovieViewModel>(movie);
        }

        public bool UpdateMovie(MovieViewModel model)
        {
            var movie = _repository.GetMovie(model.Id);
            if (movie == null) return false;

            movie.Title = model.Title;
            movie.Genre = model.Genre;
            movie.ReleaseYear = model.ReleaseYear;
            movie.Watched = model.Watched;
            movie.UpdatedTime = DateTime.UtcNow;

            _repository.UpdateMovie(movie);
            return true;
        }

        public bool DeleteMovie(int id)
        {
            var movie = _repository.GetMovie(id);
            if (movie == null) return false;

            _repository.DeleteMovie(movie);
            return true;
        }
    }
}
