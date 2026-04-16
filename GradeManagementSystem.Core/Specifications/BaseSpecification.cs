using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace GradeManagementSystem.Core.Specifications
{
    public class BaseSpecification<T> :ISpecification<T> where T : class
    {
        public Expression<Func<T, bool>> Condtion { get; set; }
        public List<Expression<Func<T, object>>> Inlude { get; set; } = new List<Expression<Func<T, object>>>();
        public List<string> ThenIncludes { get; set; } = new List<string>(); 
        public BaseSpecification()
        {
            
        }
        public BaseSpecification(Expression<Func<T, bool>> expression)
        {
            Condtion = expression;
            
        }


    }
}
