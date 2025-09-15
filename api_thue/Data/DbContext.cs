using Microsoft.EntityFrameworkCore;
using api_thue.DTOs;
using Microsoft.Data.SqlClient;

namespace api_thue.Data
{
    public class ApplicationDbContext 
    {
        private readonly string _connectionString;

        public ApplicationDbContext(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<int> SaveDoanhNghiepAsync(CompanyInfo dn)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            //var cmd = new SqlCommand(@"
            //    INSERT INTO CompanyInfo
            //        (MaSoThue, TenNguoiNopThue, DiaChi, QuanLyThue, TrangThaiMST)
            //    VALUES (@MaSoThue, @TenNguoiNopThue, @DiaChi, @QuanLyThue, @TrangThaiMST);
            //    SELECT SCOPE_IDENTITY();", conn);


            //cmd.Parameters.AddWithValue("@MaSoThue", dn.MaSoThue ?? (object)DBNull.Value);
            //cmd.Parameters.AddWithValue("@TenNguoiNopThue", dn.TenNguoiNopThue ?? (object)DBNull.Value);
            //cmd.Parameters.AddWithValue("@DiaChi", dn.DiaChi ?? (object)DBNull.Value);
            //cmd.Parameters.AddWithValue("@QuanLyThue", dn.QuanLyThue ?? (object)DBNull.Value);
            //cmd.Parameters.AddWithValue("@TrangThaiMST", dn.TrangThaiMST ?? (object)DBNull.Value);

            var cmd = new SqlCommand(@"
                INSERT INTO CompanyInfo
                    (TaxCode, CompanyName, Address, Status)
                VALUES (@TaxCode, @CompanyName, @Address, @Status);
                SELECT SCOPE_IDENTITY();", conn);

            cmd.Parameters.AddWithValue("@TaxCode", dn.MaSoThue ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@CompanyName", dn.TenNguoiNopThue ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Address", dn.DiaChi ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@Status", dn.TrangThaiMST ?? (object)DBNull.Value);


            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(Convert.ToDecimal(result));
        }

        public async Task<bool> CheckExistAsync(string maSoThue)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            //var cmd = new SqlCommand("SELECT COUNT(*) FROM CompanyInfo WHERE MaSoThue = @mst", conn);
            var cmd = new SqlCommand("SELECT COUNT(*) FROM CompanyInfo WHERE TaxCode = @mst", conn);

            cmd.Parameters.AddWithValue("@mst", maSoThue);

            var count = (int)await cmd.ExecuteScalarAsync();
            return count > 0;
        }

        public async Task<List<CompanyInfo>> GetAllAsync()
        {
            var list = new List<CompanyInfo>();
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new SqlCommand("SELECT * FROM CompanyInfo", conn);

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new CompanyInfo
                {
                    Id = reader.GetInt32(0),// Id
                    MaSoThue = reader.GetString(1),// TaxCode
                    TenNguoiNopThue = reader.IsDBNull(2) ? null : reader.GetString(2),// CompanyName
                    DiaChi = reader.IsDBNull(3) ? null : reader.GetString(3),// Address
                    QuanLyThue = reader.IsDBNull(4) ? null : reader.GetString(4),
                    TrangThaiMST = reader.IsDBNull(5) ? null : reader.GetString(5)// Status
                });
            }

            return list;
        }

    }
}
