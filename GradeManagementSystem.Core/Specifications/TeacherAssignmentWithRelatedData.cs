using GradeManagementSystem.Core.Entities.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradeManagementSystem.Core.Specifications
{
    public class TeacherAssignmentWithRelatedData:BaseSpecification<TeacherAssignment>
    {
        public TeacherAssignmentWithRelatedData():base()
        {
            Includes.Add(T=>T.Teacher);
            Includes.Add(AY=>AY.AcademicYear);
            Includes.Add(C=>C.Class);
            Includes.Add(S=>S.Subject);
           
        }
        public TeacherAssignmentWithRelatedData(int id):base(i=>i.TeacherAssignmentID==id) 
        {
            Includes.Add(T => T.Teacher);
            Includes.Add(AY => AY.AcademicYear);
            Includes.Add(C => C.Class);
            Includes.Add(S => S.Subject);
        }
    }
}
