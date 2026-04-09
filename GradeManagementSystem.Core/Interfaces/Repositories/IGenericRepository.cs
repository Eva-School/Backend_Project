using GradeManagementSystem.Repository.Specifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradeManagementSystem.Repository.Repositories
{
    public interface IGenericRepository<T> where T : class
    {
        Task<IEnumerable<T>> GetAll(ISpecification<T> specification);
        Task<T> GetById(ISpecification<T> specification);
            
        Task Add(T item);
        Task Delete (T item);
        Task Update(T item);
    }
}
