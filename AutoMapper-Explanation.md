# How AutoMapper Works in This Project

A teaching guide using the `Movie` feature in `ASI.Basecode` as the running example.

---

## 1. The Problem AutoMapper Solves

In a layered app we deliberately keep **two different shapes of the same idea**:

| Shape | Where it lives | Purpose |
|---|---|---|
| `Movie` (Entity / Model) | `ASI.Basecode.Data/Models/Movie.cs` | Mirrors the **database table**. Has `CreatedTime`, `UpdatedTime`, DB keys, etc. |
| `MovieViewModel` (DTO / ViewModel) | `ASI.Basecode.Services/ServiceModels/MovieViewModel.cs` | Mirrors what the **client (React) sends and receives**. Has validation attributes, no DB noise. |

Compare them:

```csharp
// Data/Models/Movie.cs  -- database shape
public class Movie
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Genre { get; set; }
    public int ReleaseYear { get; set; }
    public bool Watched { get; set; }
    public DateTime CreatedTime { get; set; }   // <-- not exposed to the client
    public DateTime UpdatedTime { get; set; }   // <-- not exposed to the client
}
```

```csharp
// Services/ServiceModels/MovieViewModel.cs  -- API shape
public class MovieViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Title is required.")]
    [StringLength(150)]
    public string Title { get; set; }

    [StringLength(50)]
    public string Genre { get; set; }

    [Range(1888, 2200)]
    public int ReleaseYear { get; set; }

    public bool Watched { get; set; }
}
```

**Why not just use one class?** Because then:

- The database schema becomes your public API contract — a column rename breaks every client.
- The client could POST `CreatedTime` and overwrite audit data (this is the classic **over-posting / mass-assignment** vulnerability).
- Validation rules for HTTP input get tangled with EF Core mapping rules.

So we need to copy data between the two shapes. Doing it by hand looks like this:

```csharp
var movie = new Movie
{
    Id          = model.Id,
    Title       = model.Title,
    Genre       = model.Genre,
    ReleaseYear = model.ReleaseYear,
    Watched     = model.Watched
};
```

That is fine for 5 properties. It becomes error-prone boilerplate at 30 properties across 20 entities. **AutoMapper automates exactly this copying step.**

---

## 2. The Core Rule: Convention Over Configuration

AutoMapper copies properties whose **names match** (case-insensitively) and whose **types are compatible**.

```
MovieViewModel.Title       ──►  Movie.Title        matched by name
MovieViewModel.Genre       ──►  Movie.Genre        matched by name
MovieViewModel.ReleaseYear ──►  Movie.ReleaseYear  matched by name
MovieViewModel.Watched     ──►  Movie.Watched      matched by name
(nothing)                  ──►  Movie.CreatedTime  left at default, we set it manually
```

You never write the assignments. You only **declare that the mapping is allowed**.

---

## 3. Step 1 — Declaring the Maps (the Profile)

File: `ASI.Basecode.WebApp/Startup.AutoMapper.cs`

```csharp
private class AutoMapperProfileConfiguration : Profile
{
    public AutoMapperProfileConfiguration()
    {
        CreateMap<UserViewModel, User>();
        CreateMap<MovieViewModel, Movie>();   // API shape  -> DB shape  (incoming)
        CreateMap<Movie, MovieViewModel>();   // DB shape   -> API shape (outgoing)
    }
}
```

Key points to teach:

1. **`Profile` is just a container** for map definitions. It keeps configuration in one discoverable place instead of scattered across the codebase.
2. **`CreateMap<TSource, TDestination>()` is directional.** `CreateMap<MovieViewModel, Movie>()` does **not** give you `Movie -> MovieViewModel`. That is why both lines exist. (There is a `.ReverseMap()` shortcut, but declaring both explicitly is clearer for learners.)
3. If you call `_mapper.Map<X>(y)` for a pair you never registered, AutoMapper throws `AutoMapperMappingException` at runtime. This is the single most common beginner error.

---

## 4. Step 2 — Registering `IMapper` in DI

Same file, `Startup.AutoMapper.cs`:

```csharp
private void ConfigureAutoMapper()
{
    var mapperConfiguration = new MapperConfiguration(config =>
    {
        config.AddProfile(new AutoMapperProfileConfiguration());
    });

    this._services.AddSingleton<IMapper>(sp => mapperConfiguration.CreateMapper());
}
```

What happens here:

- `MapperConfiguration` reads the profile **once at startup** and compiles the mapping rules into fast delegates. This is why AutoMapper is not as slow as naive reflection.
- The resulting `IMapper` is registered as a **singleton** — it is stateless and thread-safe, so one instance serves every request.
- `ConfigureAutoMapper()` is called from `ConfigureServices` in `Startup.cs`.

---

## 5. Step 3 — Injecting and Using It

`MovieService` asks the DI container for `IMapper`; it never news one up:

```csharp
public class MovieService : IMovieService
{
    private readonly IMovieRepository _repository;
    private readonly IMapper _mapper;

    public MovieService(IMovieRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }
```

### Case A — Incoming: `MovieViewModel` ➜ `Movie`

```csharp
public MovieViewModel AddMovie(MovieViewModel model)
{
    var movie = _mapper.Map<Movie>(model);   // creates a NEW Movie, copies matching props

    movie.Id = 0;                            // let MySQL assign the identity
    movie.CreatedTime = DateTime.UtcNow;     // server-controlled, never from the client
    movie.UpdatedTime = DateTime.UtcNow;

    _repository.AddMovie(movie);

    return _mapper.Map<MovieViewModel>(movie); // map back so the client sees the new Id
}
```

Read this line by line with students:

- `_mapper.Map<Movie>(model)` — "make me a `Movie` out of this `model`". AutoMapper allocates a new `Movie` and fills `Title`, `Genre`, `ReleaseYear`, `Watched`.
- `CreatedTime` / `UpdatedTime` were **not** copied (no matching source property), so we set them ourselves. **This is the security benefit made concrete:** the client physically cannot forge audit timestamps, because `MovieViewModel` has no such property to bind to.
- After `AddMovie`, EF Core has populated `movie.Id` from the database. We map **back** to `MovieViewModel` so the response contains the real generated id.

### Case B — Outgoing: `Movie` ➜ `MovieViewModel`

```csharp
public MovieViewModel GetMovie(int id)
{
    var movie = _repository.GetMovie(id);
    return movie == null ? null : _mapper.Map<MovieViewModel>(movie);
}
```

The entity coming out of the database gets reduced to the safe, client-facing shape. `CreatedTime` and `UpdatedTime` are simply dropped, because the destination has nowhere to put them.

---

## 6. The Deliberate Exception: `UpdateMovie` and `GetMovies`

Notice these two methods **do not** use the mapper. That is intentional, and worth explaining.

### `UpdateMovie` — updating a *tracked* entity

```csharp
public bool UpdateMovie(MovieViewModel model)
{
    var movie = _repository.GetMovie(model.Id);
    if (movie == null) return false;

    movie.Title       = model.Title;
    movie.Genre       = model.Genre;
    movie.ReleaseYear = model.ReleaseYear;
    movie.Watched     = model.Watched;
    movie.UpdatedTime = DateTime.UtcNow;

    _repository.UpdateMovie(movie);
    return true;
}
```

Why manual?

- `_mapper.Map<Movie>(model)` creates a **brand new** `Movie` whose `CreatedTime` would be `default` (`0001-01-01`). Saving that would **wipe the original creation date**.
- We want to update the entity EF Core is already tracking, preserving the fields the client isn't allowed to touch.

> AutoMapper *does* have an overload for this — `_mapper.Map(model, movie)` maps **onto an existing object**. It would work here, but writing the assignments explicitly makes the "which fields may the client change?" decision visible in the code. That explicitness is a legitimate design choice, and a good discussion point with students.

### `GetMovies` — projecting a query

```csharp
public List<MovieViewModel> GetMovies()
{
    return _repository.GetMovies()
                      .OrderBy(m => m.Title)
                      .Select(m => new MovieViewModel { Id = m.Id, Title = m.Title, /* ... */ })
                      .ToList();
}
```

Why manual? Because this `Select` runs **inside the LINQ-to-Entities query**. EF Core translates it into SQL that selects only those five columns:

```sql
SELECT Id, Title, Genre, ReleaseYear, Watched FROM Movies ORDER BY Title;
```

If we had loaded full entities and then called `_mapper.Map<List<MovieViewModel>>(...)`, the SQL would have fetched every column including the timestamps, then thrown half of it away.

> AutoMapper's `ProjectTo<MovieViewModel>(configuration)` extension does exactly this translation for you, and is the idiomatic solution for list queries.

**Rule of thumb to give students:**
- Mapping a single in-memory object → `Map`.
- Mapping a database *query* → project in the query (`Select` or `ProjectTo`).
- Updating an existing tracked entity → assign explicitly (or `Map(source, destination)`).

---

## 7. Where It Sits in the Request Flow

```
React  ──JSON──►  MovieController  ──MovieViewModel──►  MovieService
                        │                                    │
                        │                          _mapper.Map<Movie>()
                        │                                    ▼
                        │                             MovieRepository
                        │                                    │
                        │                                    ▼
                        │                              MySQL  Movies
                        │                                    │
                        │                                    ▼
                        │                          _mapper.Map<MovieViewModel>()
                        ◄────────MovieViewModel───────────────┘
React  ◄──JSON──
```

AutoMapper lives **only at the Service boundary**. The controller speaks `MovieViewModel`, the repository speaks `Movie`, and the service is the translator in the middle. Keeping the mapper out of controllers and repositories is what keeps the layers independent.

---

## 8. Common Mistakes (worth showing students on purpose)

| Symptom | Cause | Fix |
|---|---|---|
| `AutoMapperMappingException: Missing type map configuration` | You called `Map<T>` for a pair with no `CreateMap` | Add the `CreateMap<A, B>()` to the profile |
| Reverse direction fails | `CreateMap` is one-way | Add the opposite `CreateMap`, or `.ReverseMap()` |
| Property silently stays `null` / `0` | Names don't match (`MovieTitle` vs `Title`) | `.ForMember(d => d.Title, o => o.MapFrom(s => s.MovieTitle))` |
| `CreatedTime` becomes `0001-01-01` after an update | Mapped to a **new** entity instead of onto the tracked one | Assign explicitly, or use `Map(source, destination)` |
| Slow list endpoints | Loaded full entities then mapped | Project in the query (`Select` / `ProjectTo`) |

---

## 9. Quick Reference

```csharp
// 1. Declare (Startup.AutoMapper.cs)
CreateMap<MovieViewModel, Movie>();
CreateMap<Movie, MovieViewModel>();

// 2. Inject (any service)
public MovieService(IMovieRepository repo, IMapper mapper) { ... }

// 3. Use
var entity = _mapper.Map<Movie>(viewModel);          // new destination object
var dto    = _mapper.Map<MovieViewModel>(entity);    // new destination object
_mapper.Map(viewModel, existingEntity);              // map ONTO an existing object

// Custom rule when names differ
CreateMap<MovieViewModel, Movie>()
    .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.MovieTitle))
    .ForMember(dest => dest.CreatedTime, opt => opt.Ignore());
```

**One-sentence summary:** AutoMapper is a configured, compiled property-copier that lets each layer keep its own class shape without you writing hundreds of assignment statements — and, as a side effect, it stops clients from writing to fields you never exposed.
