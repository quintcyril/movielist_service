using ASI.Basecode.Data.Interfaces;
using ASI.Basecode.Data.Models;
using Basecode.Data.Repositories;
using System.Linq;

namespace ASI.Basecode.Data.Repositories
{
    public class MovieRepository : BaseRepository, IMovieRepository
    {
        public MovieRepository(IUnitOfWork unitOfWork) : base(unitOfWork)
        {
        }

        public IQueryable<Movie> GetMovies()
        {
            return this.GetDbSet<Movie>();
        }

        public Movie GetMovie(int id)
        {
            return this.GetDbSet<Movie>().FirstOrDefault(m => m.Id == id);
        }

        public void AddMovie(Movie movie)
        {
            this.GetDbSet<Movie>().Add(movie);
            UnitOfWork.SaveChanges();
        }

        public void UpdateMovie(Movie movie)
        {
            this.GetDbSet<Movie>().Update(movie);
            UnitOfWork.SaveChanges();
        }

        public void DeleteMovie(Movie movie)
        {
            this.GetDbSet<Movie>().Remove(movie);
            UnitOfWork.SaveChanges();
        }
    }
}
