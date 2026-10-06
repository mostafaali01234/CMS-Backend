using CMS.Api.Middleware;
using CMS.Api.Services;
using CMS.Application.DTOs;
using CMS.Application.Interfaces;
using CMS.Application.Interfaces.Configuration;
using CMS.Application.Services;
using CMS.Infrastructure.Auditing;
using CMS.Infrastructure.Identity;
using CMS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var key = Encoding.ASCII.GetBytes(builder.Configuration["JwtConfig:Secret"]);
var tokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuerSigningKey = true,
    IssuerSigningKey = new SymmetricSecurityKey(key),
    ValidateIssuer = false,
    ValidateAudience = false,
    RequireExpirationTime = false,
    ValidateLifetime = true,

    // Allow to use seconds for expiration of token
    // Required only when token lifetime less than 5 minutes
    // THIS ONE
    ClockSkew = TimeSpan.Zero
};
builder.Services.AddSingleton(tokenValidationParameters);
builder.Services.AddAuthentication(options => {
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(jwt => {
    jwt.SaveToken = true;
    jwt.TokenValidationParameters = tokenValidationParameters;
});
builder.Services.AddAuthorization(options => {
    options.AddPolicy("DepartmentPolicy", 
        policy => policy.RequireClaim("Department"));
});


builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
        options.SignIn.RequireConfirmedAccount = true
        ).AddEntityFrameworkStores<AppDbContext>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();

//builder.Services.AddScoped<IAppDbContext>(p => p.GetRequiredService<AppDbContext>());
builder.Services.AddScoped<IAppDbContext, AppDbContext>();
builder.Services.AddScoped<AuditSaveChangesInterceptor>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IUserRoleService, UserRoleService>();
builder.Services.AddScoped<IUserClaimService, UserClaimService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<IJobService, JobService>();
builder.Services.AddScoped<IStateService, StateService>();
builder.Services.AddScoped<ICityService, CityService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IEmployeeCommissionService, EmployeeCommissionService>();
builder.Services.AddScoped<IEmployeeSpecialCommissionService, EmployeeSpecialCommissionService>();
builder.Services.AddScoped<IEmployeeSpecialCommissionDefinitionService, EmployeeSpecialCommissionDefinitionService>();
builder.Services.AddScoped<IEmployeePayrollAdjustmentService, EmployeePayrollAdjustmentService>();
builder.Services.AddScoped<IEmployeePayrollService, EmployeePayrollService>();
builder.Services.AddScoped<IEmployeeLoanService, EmployeeLoanService>();
builder.Services.AddScoped<IEmployeeLoanInstallmentService, EmployeeLoanInstallmentService>();
builder.Services.AddScoped<IProductCategoryService, ProductCategoryService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IProductAssemblyDefinitionService, ProductAssemblyDefinitionService>();
builder.Services.AddScoped<IProductAssemblyOperationService, ProductAssemblyOperationService>();
builder.Services.AddScoped<IProductUnitCommissionService, ProductUnitCommissionService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddScoped<ISupplierPaymentService, SupplierPaymentService>();
builder.Services.AddScoped<IBuyInvoiceService, BuyInvoiceService>();
builder.Services.AddScoped<ISaleInvoiceService, SaleInvoiceService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<ICustomerPaymentService, CustomerPaymentService>();
builder.Services.AddScoped<ICarService, CarService>();
builder.Services.AddScoped<IStoreService, StoreService>();
builder.Services.AddScoped<IStoreTransactionService, StoreTransactionService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IExpenseCategoryService, ExpenseCategoryService>();
builder.Services.AddScoped<IExpenseTypeService, ExpenseTypeService>();
builder.Services.AddScoped<IExpenseService, ExpenseService>();
builder.Services.AddScoped<IOrderLineService, OrderLineService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IOrderTechHistoryService, OrderTechHistoryService>();
builder.Services.AddScoped<IOrderNoteHistoryService, OrderNoteHistoryService>();
builder.Services.AddScoped<IMoneySafeCategoryService, MoneySafeCategoryService>();
builder.Services.AddScoped<IMoneySafeService, MoneySafeService>();
builder.Services.AddScoped<IMoneySafeTransactionService, MoneySafeTransactionService>();
builder.Services.AddScoped<IUserLookup, UserLookup>();

builder.Services.AddCorrelationIdManager();


builder.Services.AddDbContext<AppDbContext>((sp, options) =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.MigrationsAssembly("CMS.Infrastructure"))
        //sql => sql.EnableRetryOnFailure())
    .AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>()));
builder.Services.Configure<JwtConfig>(builder.Configuration.GetSection("JwtConfig"));

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter());
    });

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddApiVersioning(opt =>
{
    opt.AssumeDefaultVersionWhenUnspecified = true;
    opt.DefaultApiVersion = Microsoft.AspNetCore.Mvc.ApiVersion.Default; // new ApiVersion(1, 0); 
});



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "My API v1");
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.UseMiddleware<ApiActivityLogMiddleware>();

app.MapControllers();

app.AddCorrelationIdMiddleware();

app.Run();
