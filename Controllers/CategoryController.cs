using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CloudProperty.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly DataContext context;

        public CategoryController(DataContext context) {
            this.context = context;
        }

        [HttpGet]
        public async Task<ActionResult<List<Category>>> Get()
        {
            return Ok(await this.context.Categories.ToListAsync());
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Category>> Get(int id)
        {
            var category = await this.context.Categories.FindAsync(id);
            if (category == null)
                return BadRequest("Category not found");
            return Ok(category);
        }

        [HttpPost]
        public async Task<ActionResult<List<Category>>> AddCategory(Category category)
        {
            this.context.Categories.Add(category);
            await this.context.SaveChangesAsync();
            return Ok(await this.context.Categories.ToListAsync());  
        }

        [HttpPut]
        public async Task<ActionResult<List<Category>>> PutCategory(Category request) 
        {
            var category = await this.context.Categories.FindAsync(request.Id);
            if (category == null)
                return BadRequest("Category not found");
            category.Description = request.Description; 
            category.ModelName = request.ModelName;

            await this.context.SaveChangesAsync();

            return Ok(await this.context.Categories.ToListAsync());
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteCategory(int id) 
        {
            var category = await this.context.Categories.FindAsync(id);
            if (category == null)
                return BadRequest("Category not found");

            this.context.Categories.Remove(category);
            await this.context.SaveChangesAsync();
            return Ok(await this.context.Categories.ToListAsync());
        }
    }
}
