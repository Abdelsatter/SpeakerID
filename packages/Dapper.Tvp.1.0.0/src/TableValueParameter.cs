using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.Server;

namespace Dapper.Tvp
{
    public class TableValueParameter : Dapper.SqlMapper.IDynamicParameters
    {
        string paramName;
        string typeName;
        IEnumerable<SqlDataRecord> rows;
        public TableValueParameter(string paramName, string typeName, IEnumerable<SqlDataRecord> rows)
        {
            this.paramName = paramName;
            this.typeName = typeName;
            this.rows = rows;
        }

        public void AddParameters(IDbCommand command, Dapper.SqlMapper.Identity identity)
        {
            SqlCommand sqlCommand = null;
            if (typeof(SqlCommand).IsAssignableFrom(command.GetType()))
                sqlCommand = command as SqlCommand;

            if (sqlCommand == null)
                throw new ArgumentException("Could not convert to a SqlCommand.", "command");

            var p = sqlCommand.Parameters.Add(paramName, SqlDbType.Structured);
            p.Direction = ParameterDirection.Input;
            p.TypeName = typeName;
            p.Value = rows;
        }
    }
}
