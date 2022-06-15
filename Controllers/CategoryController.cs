using CloudProperty.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CloudProperty.Controllers
{
    [Route("api/category")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly DataContext context;

        public CategoryController(DataContext context) {
            this.context = context;
        }

        [HttpGet, Authorize]
        public async Task<ActionResult<List<Category>>> Get()
        {
            //string authUserId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier).Value;
            //var user = new User().GetUserById(Convert.ToInt32(authUserId));

            //return Ok(user);

            return Ok(await this.context.Categories.ToListAsync());
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Category>> Get(int id)
        {
            var category = await this.context.Categories.FindAsync(id);
            if (category == null) 
            {
                return BadRequest("Category not found");
            }
            return Ok(category);
        }

        [HttpPost]
        public async Task<ActionResult<List<Category>>> AddCategory(Category category)
        {
            category.CreatedAt = DateTime.UtcNow;
            category.UpdatedAt = DateTime.UtcNow;
            this.context.Categories.Add(category);

            await this.context.SaveChangesAsync();
            
            return Ok(await this.context.Categories.ToListAsync());  
        }

        [HttpPut]
        public async Task<ActionResult<List<Category>>> PutCategory(Category request) 
        {
            var category = await this.context.Categories.FindAsync(request.Id);
            if (category == null)
            {
                return BadRequest("Category not found");
            }

            category.Description = request.Description; 
            category.ModelName = request.ModelName;
            category.UpdatedAt = DateTime.UtcNow;

            await this.context.SaveChangesAsync();

            return Ok(await this.context.Categories.ToListAsync());
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteCategory(int id) 
        {
            var category = await this.context.Categories.FindAsync(id);
            if (category == null)
            {
                return BadRequest("Category not found");
            }

            this.context.Categories.Remove(category);
            
            await this.context.SaveChangesAsync();

            return Ok(await this.context.Categories.ToListAsync());
        }
    }
}
