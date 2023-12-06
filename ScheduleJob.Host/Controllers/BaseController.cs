using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using OneForAll.Core.Extension;
using ScheduleJob.Domain.Models;
using ScheduleJob.Public.Models;
using OneForAll.Core;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using OneForAll.File;
using System.IO;
using OneForAll.Core.Utility;
using OneForAll.Core.Security;

namespace ScheduleJob.Host.Controllers
{
    public class BaseController : Controller
    {
        protected Guid UserId
        {
            get
            {
                var userId = HttpContext
                .User
                .Claims
                .FirstOrDefault(e => e.Type == UserClaimType.USER_ID);

                if (userId != null)
                {
                    return new Guid(userId.Value);
                }
                return Guid.Empty;
            }
        }

        protected string UserName
        {
            get
            {
                var username = HttpContext
                .User
                .Claims
                .FirstOrDefault(e => e.Type == UserClaimType.USERNAME);

                if (username != null)
                {
                    return username.Value;
                }
                return null;
            }
        }

        protected Guid TenantId
        {
            get
            {
                var tenantId = HttpContext
                .User
                .Claims
                .FirstOrDefault(e => e.Type == UserClaimType.TENANT_ID);

                if (tenantId != null)
                {
                    return new Guid(tenantId.Value);
                }
                return Guid.Empty;
            }
        }

        protected new LoginUser User
        {
            get
            {
                var name = HttpContext
                .User
                .Claims
                .FirstOrDefault(e => e.Type == UserClaimType.USER_NICKNAME);

                var role = HttpContext
                .User
                .Claims
                .FirstOrDefault(e => e.Type == UserClaimType.ROLE);

                return new LoginUser()
                {
                    Id = UserId,
                    Name = name.Value,
                    SysTenantId = TenantId
                };
            }
        }

        public static string GetModelStateFirstError(ModelStateDictionary modelState)
        {
            var error = modelState.Where(m => m.Value.Errors.Any())
                .Select(x => new { x.Key, x.Value.Errors }).FirstOrDefault().Errors.First();
            return error.ErrorMessage.IsNullOrEmpty() ? error.Exception.Message : error.ErrorMessage;
        }

        /// <summary>
        /// 导入Excel
        /// </summary>
        /// <typeparam name="TImport">可导入的类型</typeparam>
        /// <param name="form">表单</param>
        /// <param name="action">自定义方法</param>
        /// <returns></returns>
        public async Task<BaseMessage> ImportExcelAsync<TImport>(IFormCollection form, Func<IEnumerable<TImport>, Task<BaseMessage>> action) where TImport : class, new()
        {
            var msg = new BaseMessage() { ErrType = BaseErrType.DataEmpty };
            var errors = new List<ValidateTableResult>();
            try
            {
                if (form.Files.Count > 0)
                {
                    var index = 0;
                    var file = form.Files.First();
                    var extension = Path.GetExtension(file.FileName);
                    var fileType = extension == ".xlsx" ? FileType.xlsx : FileType.xls;
                    var dts = NPOIExcelHelper.Import(file.OpenReadStream(), fileType, true);
                    if (dts.Count() > 0)
                    {
                        msg.ErrType = BaseErrType.DataError;
                        var dt = dts.First();
                        var data = ReflectionHelper.ToList<TImport>(dt, out errors);
                        data.ForEach(e =>
                        {
                            if (!TryValidateModel(e))
                            {
                                var item = errors.FirstOrDefault(w => w.RowIndex == index);
                                if (item == null)
                                {
                                    item = new ValidateTableResult()
                                    {
                                        RowIndex = index,
                                        Source = dt.Rows[index].ItemArray
                                    };
                                    errors.Add(item);
                                }
                                var modelErrors = ModelState.Where(m => m.Value.Errors.Any()).Select(x => new { x.Key, x.Value.Errors }).ToList();
                                var props = ReflectionHelper.GetPropertys(e);
                                modelErrors.ForEach(err =>
                                {
                                    var columnIndex = 0;
                                    for (int i = 0; i < props.Length; i++)
                                    {
                                        if (props[i].Name == err.Key)
                                        {
                                            columnIndex = i;
                                            break;
                                        }
                                    }
                                    var result = new ValidateTableColumnResult()
                                    {
                                        ColumnIndex = columnIndex,
                                        Error = err.Errors.First().ErrorMessage
                                    };
                                    item.Columns.Add(result);
                                });
                                ModelState.Clear();
                            }
                            index++;
                        });
                        if (errors.Count < 1)
                        {
                            msg = await action(data);
                            if (msg.Data != null)
                            {
                                var dbErrors = msg.Data as List<ValidateTableResult>;
                                dbErrors.ForEach(e =>
                                {
                                    e.Source = dt.Rows[e.RowIndex].ItemArray;
                                });
                            }
                        }
                        else
                        {
                            var sumErrors = new List<ValidateTableResult>();
                            errors.ForEach(e =>
                            {
                                var row = sumErrors.FirstOrDefault(w => w.RowIndex == e.RowIndex);
                                if (row != null)
                                {
                                    e.Columns.ForEach(e2 => { row.Columns.Add(e2); });
                                }
                                else
                                {
                                    sumErrors.Add(e);
                                }
                            });
                            msg.Data = sumErrors;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                msg.ErrType = BaseErrType.DataError;
                msg.Message = ex.Message;
            }
            return msg;
        }

        /// <summary>
        /// 校验签名
        /// </summary>
        /// <param name="appId"></param>
        /// <param name="appSecret"></param>
        /// <param name="sign"></param>
        /// <returns></returns>
        protected bool VerifySign(string appId, string appSecret, string sign)
        {
            var key1 = Md5Helper.Encrypt(appId + appSecret + DateTime.Now.ToString("yyyyMMddHHmm"));
            var key2 = Md5Helper.Encrypt(appId + appSecret + DateTime.Now.AddMinutes(-1).ToString("yyyyMMddHHmm"));
            return sign == key1 || sign == key2;
        }
    }
}