using Azunt.DepartmentManagement;
using Azunt.Models.Enums;
using Azunt.Web.Components;
using Azunt.Web.Components.Account;
using Azunt.Web.Data;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();


// =======================================================
// Department 모듈 Sqlite용 DI 등록 (두 가지 방식 중 하나만 사용)
// =======================================================

// 2. 직접 등록 방식
//builder.Services.AddDbContext<DepartmentDbContext>(
//    options => options.UseSqlite(connectionString),  // Sqlite 사용
//    ServiceLifetime.Transient);
//
//builder.Services.AddTransient<IDepartmentRepository, DepartmentRepository>();
//builder.Services.AddTransient<DepartmentDbContextFactory>();

// 2. 확장 메서드 방식 (권장)
builder.Services.AddDependencyInjectionContainerForDepartmentApp(
    connectionString,
    RepositoryMode.EfCore,          // EF Core 모드
    DbProvider.Sqlite,              // Sqlite Provider
    ServiceLifetime.Transient);     // DbContext Lifetime

// =======================================================


var app = builder.Build();


// -------------------------------------------
// Sqlite 전용 Departments 테이블 생성/업데이트 호출
// -------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    // Master DB용
    DepartmentsTableBuilderSqlite.Run(services, forMaster: true);

    // Tenant DB용 (멀티테넌트)
    //DepartmentsTableBuilderSqlite.Run(services, forMaster: false);
}
// -------------------------------------------


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();
