using System.Text;
using KahveDostum_Service.Application.Interfaces;
using KahveDostum_Service.Application.Services;
using KahveDostum_Service.Domain.Interfaces;
using KahveDostum_Service.Infrastructure.Data;
using KahveDostum_Service.Infrastructure.Repositories;
using KahveDostum_Service.Extensions;
using KahveDostum_Service.Infrastructure.Background;
using KahveDostum_Service.Infrastructure.Options;
using KahveDostum_Service.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------
// CORS
// ---------------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ---------------------------
// DbContext
// ---------------------------
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ---------------------------
// MinIO + Rabbit Options
// ---------------------------
builder.Services.Configure<MinioOptions>(builder.Configuration.GetSection("Minio"));
builder.Services.Configure<RabbitOptions>(builder.Configuration.GetSection("Rabbit"));
builder.Services.Configure<RealtimeOptions>(builder.Configuration.GetSection("Realtime"));

// ---------------------------
// JWT
// ---------------------------
var jwtSection = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSection["Key"]!);

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
// ---------------------------
// Infrastructure Services (MinIO + Rabbit)
// ---------------------------
builder.Services.AddSingleton<IObjectStorage, MinioStorage>();
builder.Services.AddSingleton<IOcrJobPublisher, RabbitOcrJobPublisher>();


// ---------------------------
// Repositories + UoW
// ---------------------------

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<ICompanyRepository, CompanyRepository>();

builder.Services.AddScoped<IReceiptRepository, ReceiptRepository>();
builder.Services.AddScoped<IReceiptLineRepository, ReceiptLineRepository>();

builder.Services.AddScoped<ICafeActivationTokenRepository, CafeActivationTokenRepository>();

builder.Services.AddScoped<IFriendRequestRepository, FriendRequestRepository>();
builder.Services.AddScoped<IFriendshipRepository, FriendshipRepository>();

builder.Services.AddScoped<IConversationRepository, ConversationRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IMessageReceiptRepository, MessageReceiptRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IReceiptOcrResultRepository, ReceiptOcrResultRepository>();

//  🔥 EKSİK OLAN 2 REPOSITORY — ZORUNLU 🔥
builder.Services.AddScoped<ICafeRepository, CafeRepository>();
builder.Services.AddScoped<IUserSessionRepository, UserSessionRepository>();

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// ---------------------------
// Services
// ---------------------------
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IFriendService, FriendService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<ICafeService, CafeService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<ICafeTokenService, CafeTokenService>();
builder.Services.AddScoped<IReceiptService, ReceiptService>();

// ---------------------------
// Controllers + Swagger
// ---------------------------
builder.Services.AddHttpClient("realtime"); 
builder.Services.AddControllers();
builder.Services.AddSwaggerDocumentation();
builder.Services.AddHostedService<ResultsConsumer>();

var app = builder.Build();

// ---------------------------
// Middleware Pipeline
// ---------------------------
app.UseSwaggerDocumentation();

app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
