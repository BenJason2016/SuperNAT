﻿using Dapper;
using SuperNAT.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace SuperNAT.Dal
{
    public class ClientDal : BaseDal<Client>
    {
        public ReturnResult<Client> GetOne(string secret, Trans t = null)
        {
            var rst = new ReturnResult<Client>() { Message = "暂无记录" };

            try
            {
                conn = CreateMySqlConnection(t);
                rst.Data = conn.QueryFirstOrDefault<Client>(@"SELECT
	                                                                t1.*, t2.user_name
                                                                FROM
	                                                                client t1
                                                                LEFT JOIN `user` t2 ON t1.user_id = t2.user_id
                                                                WHERE
	                                                                t1.secret =@secret", new { secret }, t?.DbTrans);
                if (rst.Data != null)
                {
                    rst.Result = true;
                    rst.Message = "获取成功";
                }
            }
            catch (Exception ex)
            {
                rst.Message = $"获取失败：{ex.InnerException ?? ex}";
                Log4netUtil.Error($"{ex.InnerException ?? ex}");
            }

            return rst;
        }

        public ReturnResult<List<Client>> GetList(Client model, Trans t = null)
        {
            var rst = new ReturnResult<List<Client>>() { Message = "暂无记录" };

            try
            {
                conn = CreateMySqlConnection(t);
                var sql = new StringBuilder(@"SELECT
	                                                t1.*, t2.user_name
                                                FROM
	                                                client t1
                                                LEFT JOIN `user` t2 ON t1.user_id = t2.user_id ");
                bool is_admin = !string.IsNullOrWhiteSpace(model.user_id) && !model.is_admin;
                if (model.page_index > 0)
                {
                    if (!string.IsNullOrWhiteSpace(model.search))
                    {
                        model.search = $"%{model.search}%";
                        sql.Append("where (t1.name like @search ");
                        sql.Append("or t1.remark like @search ");
                        sql.Append("or t2.user_name like @search) ");
                        sql.Append(is_admin ? "and t2.user_id = @user_id " : "");
                    }
                    else
                    {
                        sql.Append(is_admin ? "where t2.user_id = @user_id " : "");
                    }
                    rst.Data = conn.GetListPaged<Client>(model.page_index, model.page_size, sql.ToString(), out int totalCount, "id asc", model, t?.DbTrans).ToList();
                    rst.PageInfo = new PageInfo()
                    {
                        PageIndex = model.page_index,
                        PageSize = model.page_size,
                        TotalCount = totalCount
                    };
                    rst.Result = true;
                    rst.Message = "获取成功";
                }
                else
                {
                    sql.Append("order by t1.id ");
                    rst.Data = conn.Query<Client>(sql.ToString(), null, t?.DbTrans).ToList();
                }
                if (rst.Data != null)
                {
                    rst.Result = true;
                    rst.Message = "获取成功";
                }
            }
            catch (Exception ex)
            {
                rst.Message = $"获取失败：{ex.InnerException ?? ex}";
                Log4netUtil.Error($"{ex.InnerException ?? ex}");
            }

            return rst;
        }

        public ReturnResult<bool> UpdateOnlineStatus(Client model, Trans t = null)
        {
            var rst = new ReturnResult<bool>() { Message = "更新失败" };

            try
            {
                conn = CreateMySqlConnection(t);
                if (conn.Execute($"update client set is_online=@is_online{(model.is_online ? ",last_heart_time=@last_heart_time" : "")} where secret=@secret", model, t?.DbTrans) > 0)
                {
                    rst.Result = true;
                    rst.Message = "更新成功";
                }
            }
            catch (Exception ex)
            {
                rst.Message = $"更新失败：{ex.InnerException ?? ex}";
                Log4netUtil.Error($"{ex.InnerException ?? ex}");
            }

            return rst;
        }

        public ReturnResult<bool> UpdateOfflineClient(Trans t = null)
        {
            var rst = new ReturnResult<bool>() { Message = "更新失败" };

            try
            {
                conn = CreateMySqlConnection(t);
                var clients = conn.GetList<Client>("where (is_online=@is_online && last_heart_time<@last_heart_time) or last_heart_time is null", new { is_online = true, last_heart_time = DateTime.Now.AddMinutes(-1) }, t?.DbTrans).Select(c => c.id).ToList();
                if (clients.Any())
                {
                    int count = conn.Execute($"update client set is_online=0 where id in({string.Join(',', clients)})");
                    if (count > 0)
                    {
                        rst.Result = true;
                        rst.Message = $"更新假在线主机成功条数：{count}";
                    }
                }
                else
                {
                    rst.Message = "暂无需要更新的假在线主机";
                }
            }
            catch (Exception ex)
            {
                rst.Message = $"更新失败：{ex.InnerException ?? ex}";
                Log4netUtil.Error($"{ex.InnerException ?? ex}");
            }

            return rst;
        }
    }
}
