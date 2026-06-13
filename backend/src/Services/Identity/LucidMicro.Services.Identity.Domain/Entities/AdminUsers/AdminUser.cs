using LucidMicro.BuildingBlocks.Domain.Core.Entities;
using LucidMicro.BuildingBlocks.Domain.Core.Guards;

namespace LucidMicro.Services.Identity.Domain.Entities.AdminUsers;

/// <summary>
/// 管理员用户
/// </summary>
public class AdminUser : SoftDeleteEntity<Guid>
{
    /// <summary>
    /// 初始化管理员用户实例
    /// </summary>
    private AdminUser()
    {
    }

    /// <summary>
    /// 初始化管理员用户实例
    /// </summary>
    /// <param name="id">资源标识</param>
    /// <param name="userName">用户名称</param>
    /// <param name="email">邮箱</param>
    /// <param name="displayName">显示名称</param>
    /// <param name="phoneNumber">手机号</param>
    /// <param name="passwordHash">密码哈希</param>
    /// <param name="isActive">是否启用</param>
    private AdminUser(
        Guid id,
        string userName,
        string email,
        string displayName,
        string? phoneNumber,
        string passwordHash,
        bool isActive)
    {
        Id = id;
        ApplyProfile(userName, email, displayName, phoneNumber);
        PasswordHash = DomainGuard.RequiredText(passwordHash, nameof(passwordHash), 2048);
        IsActive = isActive;
    }

    /// <summary>
    /// 用户名称
    /// </summary>
    public string UserName { get; private set; } = string.Empty;

    /// <summary>
    /// 邮箱
    /// </summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>
    /// 显示名称
    /// </summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>
    /// 手机号
    /// </summary>
    public string? PhoneNumber { get; private set; }

    /// <summary>
    /// 密码哈希
    /// </summary>
    public string PasswordHash { get; private set; } = string.Empty;

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// 最近登录时间
    /// </summary>
    public DateTime? LastLoginAt { get; private set; }

    /// <summary>
    /// 创建实例
    /// </summary>
    /// <param name="id">资源标识</param>
    /// <param name="userName">用户名称</param>
    /// <param name="email">邮箱</param>
    /// <param name="displayName">显示名称</param>
    /// <param name="phoneNumber">手机号</param>
    /// <param name="passwordHash">密码哈希</param>
    /// <param name="isActive">是否启用</param>
    /// <returns>返回处理结果</returns>
    public static AdminUser Create(
        Guid id,
        string userName,
        string email,
        string displayName,
        string? phoneNumber,
        string passwordHash,
        bool isActive)
    {
        return new AdminUser(id, userName, email, displayName, phoneNumber, passwordHash, isActive);
    }

    /// <summary>
    /// 更新
    /// </summary>
    /// <param name="userName">用户名称</param>
    /// <param name="email">邮箱</param>
    /// <param name="displayName">显示名称</param>
    /// <param name="phoneNumber">手机号</param>
    /// <param name="isActive">是否启用</param>
    /// <returns>返回处理结果</returns>
    public void Update(
        string userName,
        string email,
        string displayName,
        string? phoneNumber,
        bool isActive)
    {
        UpdateProfile(userName, email, displayName, phoneNumber);

        if (isActive)
        {
            Activate();
            return;
        }

        Deactivate();
    }

    /// <summary>
    /// 更新资料
    /// </summary>
    /// <param name="userName">用户名称</param>
    /// <param name="email">邮箱</param>
    /// <param name="displayName">显示名称</param>
    /// <param name="phoneNumber">手机号</param>
    public void UpdateProfile(
        string userName,
        string email,
        string displayName,
        string? phoneNumber)
    {
        ApplyProfile(userName, email, displayName, phoneNumber);
    }

    /// <summary>
    /// 更新密码
    /// </summary>
    /// <param name="passwordHash">密码哈希</param>
    public void ChangePassword(string passwordHash)
    {
        PasswordHash = DomainGuard.RequiredText(passwordHash, nameof(passwordHash), 2048);
    }

    /// <summary>
    /// 启用资源
    /// </summary>
    public void Activate()
    {
        IsActive = true;
    }

    /// <summary>
    /// 禁用资源
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>
    /// 标记登录时间
    /// </summary>
    /// <param name="lastLoginAt">最近登录时间</param>
    public void MarkLogin(DateTime lastLoginAt)
    {
        LastLoginAt = lastLoginAt;
    }

    /// <summary>
    /// 应用客户基本资料
    /// </summary>
    /// <param name="userName">用户名称</param>
    /// <param name="email">邮箱</param>
    /// <param name="displayName">显示名称</param>
    /// <param name="phoneNumber">手机号</param>
    private void ApplyProfile(string userName, string email, string displayName, string? phoneNumber)
    {
        UserName = DomainGuard.RequiredText(userName, nameof(userName), 64);
        Email = DomainGuard.RequiredText(email, nameof(email), 256);
        DisplayName = DomainGuard.RequiredText(displayName, nameof(displayName), 128);
        PhoneNumber = DomainGuard.OptionalText(phoneNumber, nameof(phoneNumber), 32);
    }
}
