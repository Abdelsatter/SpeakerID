using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dapper.Tvp
{
    public class DynamicParametersTvp : DynamicParameters, Dapper.SqlMapper.IDynamicParameters
    {
        IList<TableValueParameter> tableValueParameters;
        public DynamicParametersTvp()
            : base()
        {
        }
        public DynamicParametersTvp(object template)
            : base(template)
        {
        }

        public void Add(TableValueParameter param)
        {
            if (tableValueParameters == null)
                tableValueParameters = new List<TableValueParameter>();

            tableValueParameters.Add(param);
        }
        void SqlMapper.IDynamicParameters.AddParameters(System.Data.IDbCommand command, SqlMapper.Identity identity)
        {
            base.AddParameters(command, identity);
            if (tableValueParameters != null)
            {
                foreach (var param in tableValueParameters)
                {
                    param.AddParameters(command, identity);
                }
            }
        }
    }
}
