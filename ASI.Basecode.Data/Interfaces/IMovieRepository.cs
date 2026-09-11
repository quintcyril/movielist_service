using ASI.Basecode.Data.Models;
using System.Linq;

namespace ASI.Basecode.Data.Interfaces
{
    public interface IMovieRepository
    {
        IQueryable<Movie> GetMovies();

        Movie GetMovie(int id);

        void AddMovie(Movie movie);

        void UpdateMovie(Movie movie);

        void DeleteMovie(Movie movie);
    }
}
