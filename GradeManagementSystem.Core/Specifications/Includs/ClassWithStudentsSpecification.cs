using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GradeManagementSystem.Core.Entities.Domain;

namespace GradeManagementSystem.Core.Specifications.Includs
{
    public class ClassWithStudentsSpecification:BaseSpecification<Class>
    {
        public ClassWithStudentsSpecification(int classId) :base(c=>c.ClassID==classId)
        {
           Inlude.Add(c => c.Students);

            ThenIncludes.Add("Students.SubjectTermResults");
            ThenIncludes.Add("Students.AllResults");

        }
    }
}
