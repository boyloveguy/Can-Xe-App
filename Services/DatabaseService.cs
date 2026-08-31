using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using CanXeApp.Models;

namespace CanXeApp.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString = "Data Source=CanXeData.db";

        public DatabaseService()
        {
            InitDatabase();
        }

        private void InitDatabase()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            string createTableSql = @"
                CREATE TABLE IF NOT EXISTS PhieuCan (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    STT INTEGER NOT NULL,
                    TramCanName TEXT,
                    TramCanAddress TEXT,
                    TramCanPhone TEXT,
                    LoaiHang TEXT,
                    KhachHang TEXT,
                    SoXe TEXT,
                    KhoiLuongTong REAL,
                    KhoiLuongXe REAL,
                    NgayInPhieu TEXT,
                    SoChungTu TEXT,
                    GhiChu TEXT,
                    KieuCan TEXT,
                    ChuHang TEXT,
                    TaiXe TEXT,
                    NguoiCan TEXT
                );";

            using var command = new SqliteCommand(createTableSql, connection);
            command.ExecuteNonQuery();

            try
            {
                string addColSql = "ALTER TABLE PhieuCan ADD COLUMN NgayInPhieu TEXT;";
                using var cmdAdd = new SqliteCommand(addColSql, connection);
                cmdAdd.ExecuteNonQuery();
            }
            catch { }
        }

        public int GetNextSTT()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            string sql = "SELECT MAX(STT) FROM PhieuCan";
            using var command = new SqliteCommand(sql, connection);
            var result = command.ExecuteScalar();
            if (result != null && result != DBNull.Value)
            {
                return Convert.ToInt32(result) + 1;
            }
            return 1;
        }

        public void SavePhieuCan(PhieuCan p)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            if (p.Id == 0)
            {
                p.STT = GetNextSTT();
                string insertSql = @"
                    INSERT INTO PhieuCan (STT, TramCanName, TramCanAddress, TramCanPhone, LoaiHang, KhachHang, SoXe, KhoiLuongTong, KhoiLuongXe, NgayInPhieu, SoChungTu, GhiChu, KieuCan, ChuHang, TaiXe, NguoiCan)
                    VALUES (@STT, @TramCanName, @TramCanAddress, @TramCanPhone, @LoaiHang, @KhachHang, @SoXe, @KhoiLuongTong, @KhoiLuongXe, @NgayInPhieu, @SoChungTu, @GhiChu, @KieuCan, @ChuHang, @TaiXe, @NguoiCan);
                    SELECT last_insert_rowid();";

                using var command = new SqliteCommand(insertSql, connection);
                AddParameters(command, p);
                p.Id = Convert.ToInt32(command.ExecuteScalar());
            }
            else
            {
                string updateSql = @"
                    UPDATE PhieuCan SET 
                        LoaiHang = @LoaiHang,
                        KhachHang = @KhachHang,
                        SoXe = @SoXe,
                        KhoiLuongTong = @KhoiLuongTong,
                        KhoiLuongXe = @KhoiLuongXe,
                        NgayInPhieu = @NgayInPhieu,
                        SoChungTu = @SoChungTu,
                        GhiChu = @GhiChu,
                        KieuCan = @KieuCan,
                        ChuHang = @ChuHang,
                        TaiXe = @TaiXe,
                        NguoiCan = @NguoiCan
                    WHERE Id = @Id;";

                using var command = new SqliteCommand(updateSql, connection);
                command.Parameters.AddWithValue("@Id", p.Id);
                AddParameters(command, p);
                command.ExecuteNonQuery();
            }
        }

        public void DeletePhieuCan(int id)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            string sql = "DELETE FROM PhieuCan WHERE Id = @Id";
            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);
            command.ExecuteNonQuery();
        }

        public List<PhieuCan> GetAllPhieuCan(string keyword = "", DateTime? fromDate = null, DateTime? toDate = null)
        {
            var list = new List<PhieuCan>();
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            string sql = "SELECT * FROM PhieuCan WHERE 1=1";

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                sql += " AND (SoXe LIKE @kw OR LoaiHang LIKE @kw OR KhachHang LIKE @kw OR CAST(STT AS TEXT) LIKE @kw)";
            }

            if (fromDate.HasValue)
            {
                sql += " AND (date(NgayInPhieu) >= date(@fromDate) OR date(NgayVao) >= date(@fromDate))";
            }

            if (toDate.HasValue)
            {
                sql += " AND (date(NgayInPhieu) <= date(@toDate) OR date(NgayVao) <= date(@toDate))";
            }

            sql += " ORDER BY Id DESC";

            using var command = new SqliteCommand(sql, connection);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                command.Parameters.AddWithValue("@kw", $"%{keyword}%");
            }

            if (fromDate.HasValue)
            {
                command.Parameters.AddWithValue("@fromDate", fromDate.Value.ToString("yyyy-MM-dd"));
            }

            if (toDate.HasValue)
            {
                command.Parameters.AddWithValue("@toDate", toDate.Value.ToString("yyyy-MM-dd"));
            }

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                string rawDateStr = "";
                int dateColIdx = -1;
                try { dateColIdx = reader.GetOrdinal("NgayInPhieu"); } catch { }
                if (dateColIdx < 0) { try { dateColIdx = reader.GetOrdinal("NgayVao"); } catch { } }

                if (dateColIdx >= 0 && !reader.IsDBNull(dateColIdx))
                {
                    rawDateStr = reader.GetString(dateColIdx);
                }

                DateTime dateVal = DateTime.Now;
                if (!string.IsNullOrEmpty(rawDateStr)) DateTime.TryParse(rawDateStr, out dateVal);

                list.Add(new PhieuCan
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    STT = reader.GetInt32(reader.GetOrdinal("STT")),
                    TramCanName = reader.GetString(reader.GetOrdinal("TramCanName")),
                    TramCanAddress = reader.GetString(reader.GetOrdinal("TramCanAddress")),
                    TramCanPhone = reader.GetString(reader.GetOrdinal("TramCanPhone")),
                    LoaiHang = reader.GetString(reader.GetOrdinal("LoaiHang")),
                    KhachHang = reader.GetString(reader.GetOrdinal("KhachHang")),
                    SoXe = reader.GetString(reader.GetOrdinal("SoXe")),
                    KhoiLuongTong = reader.GetDouble(reader.GetOrdinal("KhoiLuongTong")),
                    KhoiLuongXe = reader.GetDouble(reader.GetOrdinal("KhoiLuongXe")),
                    NgayInPhieu = dateVal,
                    SoChungTu = reader.GetString(reader.GetOrdinal("SoChungTu")),
                    GhiChu = reader.GetString(reader.GetOrdinal("GhiChu")),
                    KieuCan = reader.GetString(reader.GetOrdinal("KieuCan")),
                    ChuHang = reader.GetString(reader.GetOrdinal("ChuHang")),
                    TaiXe = reader.GetString(reader.GetOrdinal("TaiXe")),
                    NguoiCan = reader.GetString(reader.GetOrdinal("NguoiCan"))
                });
            }
            return list;
        }

        private void AddParameters(SqliteCommand command, PhieuCan p)
        {
            command.Parameters.AddWithValue("@STT", p.STT);
            command.Parameters.AddWithValue("@TramCanName", p.TramCanName ?? "");
            command.Parameters.AddWithValue("@TramCanAddress", p.TramCanAddress ?? "");
            command.Parameters.AddWithValue("@TramCanPhone", p.TramCanPhone ?? "");
            command.Parameters.AddWithValue("@LoaiHang", p.LoaiHang ?? "");
            command.Parameters.AddWithValue("@KhachHang", p.KhachHang ?? "");
            command.Parameters.AddWithValue("@SoXe", p.SoXe ?? "");
            command.Parameters.AddWithValue("@KhoiLuongTong", p.KhoiLuongTong);
            command.Parameters.AddWithValue("@KhoiLuongXe", p.KhoiLuongXe);
            command.Parameters.AddWithValue("@NgayInPhieu", p.NgayInPhieu.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.AddWithValue("@SoChungTu", p.SoChungTu ?? "");
            command.Parameters.AddWithValue("@GhiChu", p.GhiChu ?? "");
            command.Parameters.AddWithValue("@KieuCan", p.KieuCan ?? "");
            command.Parameters.AddWithValue("@ChuHang", p.ChuHang ?? "");
            command.Parameters.AddWithValue("@TaiXe", p.TaiXe ?? "");
            command.Parameters.AddWithValue("@NguoiCan", p.NguoiCan ?? "");
        }
    }
}
