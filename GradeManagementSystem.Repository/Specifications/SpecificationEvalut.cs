using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GradeManagementSystem.Core.Specifications;
using Microsoft.EntityFrameworkCore;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace GradeManagementSystem.Repository.Specifications
{
    public static class SpecificationEvalut <T> where T : class
    {
        public static IQueryable<T> GetQuery(IQueryable<T> inputquery,ISpecification<T> spec)
        {
            var Query = inputquery;

            if (spec.Condtion !=null)
            {
                Query = Query.Where(spec.Condtion);


            }

            Query = spec.Inlude.Aggregate(Query, (curr, includ) => curr.Include(includ));


            Query = spec.ThenIncludes.Aggregate(Query, (curr, include) => curr.Include(include));

            return Query;

        }

    }
}
