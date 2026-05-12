using DCafe.Models;

namespace DCafe.Services
{
    /// <summary>
    /// Provides services for managing and retrieving product categories.
    /// </summary>
    public class CategoryService : BaseService
    {
        /// <summary>
        /// Asynchronously retrieves a collection of all available product categories.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation.
        /// The task result contains an enumerable collection of <see cref="Category"/> objects.
        /// </returns>
       public async Task<IEnumerable<Category>> GetCategoriesAsync()
{
    return new List<Category>
    {
        new Category { Name = "coffee" },
        new Category { Name = "cold_brew" },
        new Category { Name = "non_coffee" },
        new Category { Name = "signature" },
        new Category { Name = "desserts" },
        new Category { Name = "meals" },
    };
}
    }
}

