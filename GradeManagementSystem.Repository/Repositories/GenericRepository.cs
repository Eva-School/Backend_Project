using GradeManagementSystem.Repository.Data;
using GradeManagementSystem.Repository.Specifications;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradeManagementSystem.Repository.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        private readonly GradeDbContext context;

        public GenericRepository(GradeDbContext context)
        {
            this.context = context;
        }

        public async Task<IEnumerable<T>> GetAll(ISpecification<T> specification)
        {
            return await BuilderSpecification<T>.GetQuery(context.Set<T>(), specification).ToListAsync();
        }

        public async Task<T> GetById(ISpecification<T> specification)
        {
            return await BuilderSpecification<T>.GetQuery(context.Set<T>(), specification).FirstOrDefaultAsync();
        }
        public async Task Add(T item)
        {
            await context.Set<T>().AddAsync(item);
            await context.SaveChangesAsync();
        }

        public async Task Delete(T item)
        {
            context.Remove(item);
            await context.SaveChangesAsync();
        }
        public async Task Update(T item)
        {
             context.Update(item);
            await context.SaveChangesAsync();
        }
    }
}
