using ASI.Basecode.Services.ServiceModels;
using System.Collections.Generic;

namespace ASI.Basecode.Services.Interfaces
{
    public interface IMovieService
    {
        List<MovieViewModel> GetMovies();

        MovieViewModel GetMovie(int id);

        MovieViewModel AddMovie(MovieViewModel model);

        bool UpdateMovie(MovieViewModel model);

        bool DeleteMovie(int id);
    }
}
