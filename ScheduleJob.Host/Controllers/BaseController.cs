using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using OneForAll.Core;
using OneForAll.Core.Extension;
using OneForAll.Core.OAuth;
using OneForAll.Core.Security;
using OneForAll.Core.Utility;
using OneForAll.File;
using OneForAll.File.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleJob.Host.Controllers
{
    public class BaseController : Controller
    {
        protected LoginUser LoginUser
        {
            get
            {
                var claims = HttpContext?.User.Claims;
                if (claims.Any())
                {
                    return new LoginUser()
                    {
                        DisplayName = claims.FirstOrDefault(e => e.Type == UserClaimType.DisplayName)?.Value ?? "",
                        UserName = claims.FirstOrDefault(e => e.Type == UserClaimType.UserName)?.Value ?? "",
                        WxAppId = claims.FirstOrDefault(e => e.Type == UserClaimType.WxAppId)?.Value ?? "",
                        WxOpenId = claims.FirstOrDefault(e => e.Type == UserClaimType.WxOpenId)?.Value ?? "",
                        WxUnionId = claims.FirstOrDefault(e => e.Type == UserClaimType.WxUnionId)?.Value ?? "",
                        Id = claims.FirstOrDefault(e => e.Type == UserClaimType.UserId)?.Value ?? "",
                        TenantId = claims.FirstOrDefault(e => e.Type == UserClaimType.TenantId)?.Value ?? "",
                        IsDefault = claims.FirstOrDefault(e => e.Type == UserClaimType.IsDefault)?.Value.TryBoolean() ?? false
                    };
                }
                return new LoginUser();
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
                    var fileType = extension == ".xlsx" ? FileTypeEnum.Xlsx : FileTypeEnum.Xls;
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
            var key1 = Md5Helper.Encrypt(appId + appSecret + DateTime.UtcNow.ToString("yyyyMMddHHmm"));
            var key2 = Md5Helper.Encrypt(appId + appSecret + DateTime.UtcNow.AddMinutes(-1).ToString("yyyyMMddHHmm"));
            return sign == key1 || sign == key2;
        }
    }
}