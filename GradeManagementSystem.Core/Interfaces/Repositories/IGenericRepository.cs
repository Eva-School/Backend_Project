using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GradeManagementSystem.Core.Specifications;

namespace GradeManagementSystem.Core.Interfaces.Repositories
{
    public interface IGenericRepository<T> where T : class
    {
        Task <IEnumerable<T>> GetAllAsync(ISpecification<T> spec);
        Task<T> GetWithIDAsync(ISpecification<T> spec);
        Task AddAsync(T Item);
        Task UpdateAsync(T Item);
        Task DeleteAsync(T Item);

    }
}
