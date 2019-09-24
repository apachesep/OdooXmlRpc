﻿using System.Net;
using System.Security.Cryptography.X509Certificates;
using CookComputing.XmlRpc;
using Odoo.Abstract;

namespace Odoo.Concrete
{
    public class RpcConnection
    {
        private readonly RpcConnectionSchema _rpcConnectionSchema;
        private IObjectRpc _objectRpc;
        private readonly RpcConnectionSetting _rpcConnectionSetting;

        public RpcConnection(RpcConnectionSetting rpcConnectionSetting)
        {
            _rpcConnectionSetting = rpcConnectionSetting;
            _rpcConnectionSchema = new RpcConnectionSchema(rpcConnectionSetting.ServerUrl, rpcConnectionSetting.DbName, rpcConnectionSetting.DbUser, rpcConnectionSetting.DbPassword);

            //TLS v.1.1 ve v.1.2 desteği
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            if (_rpcConnectionSetting.ImmediateLogin) Login();
        }

        public bool Login()
        {
            if (_rpcConnectionSchema.UserId != -1) return true;

            var loginRpc = XmlRpcProxyGen.Create<ICommonRpc>();
            loginRpc.Url = _rpcConnectionSchema.CommonUrl;

            if (_rpcConnectionSetting.ServerCertificateValidation)
            {
                ServicePointManager.ServerCertificateValidationCallback = CheckValidationResult;
            }

            // Log in and get user id
            _rpcConnectionSchema.UserId = loginRpc.login(_rpcConnectionSchema.DbName, _rpcConnectionSchema.DbUser, _rpcConnectionSchema.DbPassword);

            // Create proxy for Object operations
            _objectRpc = XmlRpcProxyGen.Create<IObjectRpc>();
            _objectRpc.Url = _rpcConnectionSchema.ObjectUrl;
            _objectRpc.NonStandard = XmlRpcNonStandard.AllowStringFaultCode;
            _objectRpc.EnableCompression = false;
            return true;
        }

        private bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            // Implement Server Certificate validation logic here, if needed.
            return true;
        }

        public int Create(string model, XmlRpcStruct fieldValues)
        {
            return _objectRpc.create(_rpcConnectionSchema.DbName, _rpcConnectionSchema.UserId, _rpcConnectionSchema.DbPassword, model, "create", fieldValues);
        }

        public int[] Search(string model, object[] filter, int? offset = null, int? limit = null)
        {
            if (offset == null || limit == null)
            {
                return _objectRpc.search(_rpcConnectionSchema.DbName, _rpcConnectionSchema.UserId, _rpcConnectionSchema.DbPassword, model, "search", filter);
            }
            return _objectRpc.search(_rpcConnectionSchema.DbName, _rpcConnectionSchema.UserId, _rpcConnectionSchema.DbPassword, model, "search", filter, (int)offset, (int)limit);
        }

        public object[] SearchAndRead(string model, object[] filter, object[] fields, int? offset = null, int? limit = null)
        {
            if (offset == null || limit == null)
            {
                return _objectRpc.search_read(_rpcConnectionSchema.DbName, _rpcConnectionSchema.UserId, _rpcConnectionSchema.DbPassword, model, "search_read", filter, fields);
            }

            return _objectRpc.search_read(_rpcConnectionSchema.DbName, _rpcConnectionSchema.UserId, _rpcConnectionSchema.DbPassword, model, "search_read", filter, fields, (int)offset, (int)limit);
        }

        public int Count(string model, object[] filter)
        {
            return _objectRpc.search_count(_rpcConnectionSchema.DbName, _rpcConnectionSchema.UserId, _rpcConnectionSchema.DbPassword, model, "search_count", filter);
        }

        public object[] Read(string model, int[] ids, object[] fields)
        {
            return _objectRpc.read(_rpcConnectionSchema.DbName, _rpcConnectionSchema.UserId, _rpcConnectionSchema.DbPassword, model, "read", ids, fields);
        }

        public bool Write(string model, int[] ids, XmlRpcStruct fieldValues)
        {
            return _objectRpc.write(_rpcConnectionSchema.DbName, _rpcConnectionSchema.UserId, _rpcConnectionSchema.DbPassword, model, "write", ids, fieldValues);
        }

        public bool Remove(string model, int[] ids)
        {
            return _objectRpc.unlink(_rpcConnectionSchema.DbName, _rpcConnectionSchema.UserId, _rpcConnectionSchema.DbPassword, model, "unlink", ids);
        }

        public bool Execute_Workflow(string model, string action, int id)
        {
            return _objectRpc.exec_workflow(_rpcConnectionSchema.DbName, _rpcConnectionSchema.UserId, _rpcConnectionSchema.DbPassword, model, action, id);
        }

        public string Render_Report(string report, int id)
        {
            return _objectRpc.render_report(_rpcConnectionSchema.DbName, _rpcConnectionSchema.UserId, _rpcConnectionSchema.DbPassword, report, id);
        }

        public RpcModel GetModel(string model)
        {
            return new RpcModel(model, this);
        }
    }
}