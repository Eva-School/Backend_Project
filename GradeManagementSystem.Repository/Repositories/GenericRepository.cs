using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GradeManagementSystem.Core.Entities.Domain;
using GradeManagementSystem.Core.Interfaces.Repositories;
using GradeManagementSystem.Core.Specifications;
using GradeManagementSystem.Repository.Data;
using GradeManagementSystem.Repository.Specifications;
using Microsoft.EntityFrameworkCore;

namespace GradeManagementSystem.Repository.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        private readonly GradeDbContext gradeDbContext;

        public GenericRepository(GradeDbContext gradeDbContext)
        {
            this.gradeDbContext = gradeDbContext;
        }

        public async Task AddAsync(T Item)
        {
            gradeDbContext.AddAsync(Item);
            await gradeDbContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(T Item)
        {
            gradeDbContext.Set<T>().Remove(Item);
            await gradeDbContext.SaveChangesAsync();
        }

        public async Task<IEnumerable<T>> GetAllAsync(ISpecification<T> spec)
        {
            return await SpecificationEvalut<T>.GetQuery(gradeDbContext.Set<T>(), spec).ToListAsync();
        }

        public async Task<T> GetWithIDAsync(ISpecification<T> spec)
        {
            return await SpecificationEvalut<T>.GetQuery(gradeDbContext.Set<T>(), spec).FirstOrDefaultAsync();

        }

        public async Task UpdateAsync(T Item)
        {
            gradeDbContext.Set<T>().Update(Item);
            await gradeDbContext.SaveChangesAsync();
        }
    }
}
