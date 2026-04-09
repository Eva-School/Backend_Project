using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradeManagementSystem.Repository.Specifications
{
    public static class BuilderSpecification<T> where T : class
    {
        public static IQueryable<T> GetQuery(IQueryable<T>startQuery,ISpecification<T> specification)
        {
            var query = startQuery;

            if (specification.Criteria is not null)
            {
                query = query.Where(specification.Criteria)  ;
            }

            query = specification.Includes.Aggregate(query, (current, nextQuery) => current.Include(nextQuery));

            return query;
        }
    }
}
