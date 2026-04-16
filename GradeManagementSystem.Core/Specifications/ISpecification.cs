using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace GradeManagementSystem.Core.Specifications
{
    public interface ISpecification<T> where T:class
    {
        public Expression<Func<T, bool>> Condtion { get; set; }
        public List<Expression<Func<T,object>>> Inlude { get; set; }
        public List<string> ThenIncludes { get; set; }
    }
}
