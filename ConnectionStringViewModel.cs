using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tonic.MVVM;
using Tonic.MVVM.Extensions;

namespace DatabaseModel
{
    public enum Providers
    {
        NpgSql,
        MicrosoftSql,
        Custom,
    }


    public class ConnectionStringViewModel : BaseViewModel
    {
        public ConnectionStringViewModel()
        {
            AddExtension(new CommandsExtension(this));
        }

        private Providers provider;
        public Providers Provider
        {
            get
            {
                return provider;
            }
            set
            {
                provider = value;
            }
        }

        public string Server { get; set; }
        public int? Port { get; set; }

        public string Database { get; set; }
        public string User { get; set; }

        public string Password { get; set; }

        private string custom;


        /// <summary>
        /// Establece los valores por default para el proveedor especificado
        /// </summary>
        public void Default()
        {
            switch (Provider)
            {
                case Providers.NpgSql:
                    Server = "localhost";
                    Port = 5432;
                    Database = "Prueba";
                    User = "postgres";
                    Password = "123456";
                    break;
            }
        }

        public string ProviderName
        {
            get
            {
                switch (Provider)
                {
                    case Providers.MicrosoftSql:
                        return "System.Data.SqlClient";
                    case Providers.NpgSql:
                        return "Npgsql";
                    default:
                        return "Custom";
                }
            }
        }

        public string ConnectionString
        {
            get
            {
                switch (Provider)
                {
                    case Providers.MicrosoftSql:
                        return $"Data Source={Server}; Initial Catalog={Database}; User Id={User}; Password={Password};";
                    case Providers.NpgSql:
                        return $"Server={Server}; Port={Port}; Database={Database}; User Id={User}; Password={Password};";
                    default:
                        return custom;
                }
            }
        }
    }
}
