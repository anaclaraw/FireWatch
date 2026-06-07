namespace FireWatch.Gateway.DTOs;

public record LoginRequest(string Email, string Password);

public record RegisterRequest(string Name, string Email, string Password);

public record RefreshRequest(string RefreshToken);

