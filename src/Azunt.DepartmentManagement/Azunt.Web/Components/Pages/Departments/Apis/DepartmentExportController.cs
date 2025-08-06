using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using Azunt.DepartmentManagement;
using System.Linq;

namespace Azunt.Apis.Departments
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize(Roles = "Administrators")]
    public class DepartmentExportController : ControllerBase
    {
        private readonly IDepartmentRepository _repository;

        public DepartmentExportController(IDepartmentRepository repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// Department 목록 엑셀 다운로드
        /// GET /api/DepartmentExport/Excel
        /// </summary>
        [HttpGet("Excel")]
        public async Task<IActionResult> ExportToExcel()
        {
            var models = await _repository.GetAllAsync();

            if (models == null || models.Count == 0)
                return NotFound("No department records found.");

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Departments");

            var range = worksheet.Cells["B2"].LoadFromCollection(
                models.Select(m => new
                {
                    m.Id,
                    m.Name,
                    CreatedAt = m.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
                    m.Active,
                    m.CreatedBy
                }),
                PrintHeaders: true
            );

            worksheet.DefaultColWidth = 25;
            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(Color.WhiteSmoke);
            range.Style.Border.BorderAround(ExcelBorderStyle.Medium);

            var header = worksheet.Cells["B2:F2"];
            header.Style.Font.Bold = true;
            header.Style.Font.Color.SetColor(Color.White);
            header.Style.Fill.BackgroundColor.SetColor(Color.DarkBlue);

            var content = package.GetAsByteArray();
            return File(content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"{DateTime.Now:yyyyMMddHHmmss}_Departments.xlsx");
        }
    }
}