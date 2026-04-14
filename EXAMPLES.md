# 代码示例：四条原则的正确与错误做法（C# / Unity）

以下示例展示如何在 C# / Unity 项目中应用 AGENTS.md 的四条原则。每个示例只展示关键差异。

## 1. 编码前思考

**原则：不要假设，主动澄清，列出多种解释。**

❌ 错误：直接写代码，假设所有细节已知。
```csharp
public void ExportUsers(string format = "json")
{
    var users = db.Users.ToList(); // 假设导出全部
    string json = JsonConvert.SerializeObject(users);
    File.WriteAllText("users.json", json);
}
```
✅ 正确：先列出假设和问题，等待用户确认。
text
需要澄清：
1. 导出范围：全部用户还是筛选条件？
2. 格式：JSON / CSV / 其他？
3. 字段：包含哪些属性（如密码字段是否排除）？
4. 数据量：是否需要分页或异步处理？
请明确后再实现。

## 2. 极简优先

**原则：最小代码，无抽象，无推测性功能。**

❌ 错误：为简单需求创建策略模式、工厂类等。
```csharp
public interface IDiscountStrategy
{
    float Calculate(float amount);
}

public class PercentageDiscount : IDiscountStrategy
{
    private float _percent;
    public PercentageDiscount(float percent) => _percent = percent;
    public float Calculate(float amount) => amount * _percent / 100;
}

public class DiscountCalculator
{
    private IDiscountStrategy _strategy;
    public DiscountCalculator(IDiscountStrategy strategy) => _strategy = strategy;
    public float Apply(float amount) => _strategy.Calculate(amount);
}
// 调用：new DiscountCalculator(new PercentageDiscount(10)).Apply(100);
```
✅ 正确：一个静态方法。
```csharp
/// <summary>
/// 计算折扣金额
/// </summary>
/// <param name="amount">原价</param>
/// <param name="percent">折扣百分比（0-100）</param>
/// <returns>折扣金额</returns>
public static float CalculateDiscount(float amount, float percent)
{
    return amount * percent / 100f;
}
// 调用：CalculateDiscount(100f, 10f)
```

## 3. 手术式改动

**原则：只改必须改的行，不顺便重构，不改变风格。**

❌ 错误：修一个 bug 时顺带重构命名、添加参数校验、改变代码格式。
```csharp
// 原方法
public void ValidateUser(UserData data)
{
    if (data.Email == null) throw new Exception("Email required");
    if (!data.Email.Contains("@")) throw new Exception("Invalid email");
    if (string.IsNullOrEmpty(data.Username)) throw new Exception("Username required");
}

// 错误改动：加了额外校验，改了异常类型，改了变量名
public void ValidateUser(UserData userData)  // 改名
{
    if (string.IsNullOrWhiteSpace(userData.Email))  // 新校验
        throw new ArgumentException("Email cannot be empty");  // 改异常类型
    if (!userData.Email.Contains('@') || !userData.Email.Contains('.'))
        throw new ArgumentException("Email format invalid");
    if (userData.Username?.Length < 3)  // 新规则
        throw new ArgumentException("Username too short");
    // ... 原有校验丢失
}
```
✅ 正确：只改动触发 bug 的那几行。
```csharp
public void ValidateUser(UserData data)
{
    // 修复：允许 null 但检查空白字符串
    if (string.IsNullOrWhiteSpace(data.Email))
        throw new Exception("Email required");
    if (!data.Email.Contains("@"))
        throw new Exception("Invalid email");
    if (string.IsNullOrEmpty(data.Username))
        throw new Exception("Username required");
}
```

## 4. 目标驱动执行

**原则：先写测试（复现 bug），再改代码，用成功标准循环验证。**

❌ 错误：直接修改代码，没有复现步骤和验证标准。
```
"我会修复分数相同时排序不一致的问题" → 直接修改 SortScores 方法 → 完成
```
✅ 正确：先写测试复现 bug，再改，最后验证。
```csharp
// 1. 编写复现问题的测试（使用 NUnit 或 Unity Test Framework）
[Test]
public void SortScores_WhenScoresAreEqual_StableOrder()
{
    var scores = new List<PlayerScore>
    {
        new PlayerScore{ Name = "Alice", Score = 100 },
        new PlayerScore{ Name = "Bob", Score = 100 },
        new PlayerScore{ Name = "Charlie", Score = 90 }
    };

    var sorted = ScoreSorter.SortScores(scores);

    // 期望 Alice 在 Bob 前面（稳定排序，按原始顺序）
    Assert.AreEqual("Alice", sorted[0].Name);
    Assert.AreEqual("Bob", sorted[1].Name);
}

// 运行测试 → 失败（因为原排序不稳定）

// 2. 修改排序方法增加二级排序
public static List<PlayerScore> SortScores(List<PlayerScore> scores)
{
    return scores
        .OrderByDescending(s => s.Score)
        .ThenBy(s => s.Name)   // 添加姓名作为稳定键
        .ToList();
}

// 3. 再次运行测试 → 通过
```

## 总结

| 原则 | 一句话规则（C# 适用） |
|------|----------------------|
| 编码前思考 | 不确定就问，列出假设，不要直接写 `new` 或 `var` |
| 极简优先 | 写最少代码（一个方法或静态类），以后需要再加接口 |
| 手术式改动 | 只改问题行，不改命名、格式、异常类型 |
| 目标驱动 | 先写 `[Test]` 复现 bug，再让测试变绿 |