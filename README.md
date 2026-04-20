# ATM Simulator

Веб-застосунок, що симулює роботу банкомату. Побудований на **ASP.NET Core MVC** та **Entity Framework Core**. Реалізує основні операції реального банкомату: автентифікацію за карткою та PIN-кодом, зняття готівки, поповнення, переказ коштів, зміну PIN-коду та адміністративну панель керування рахунками.

---

## Функціональність

- **Автентифікація** — введення номера картки та PIN-коду для входу
- **Блокування картки** — автоматичне блокування після 3 невірних спроб введення PIN
- **Зняття готівки** — жадібний алгоритм підбирає оптимальну комбінацію купюр з каси банкомату
- **Поповнення рахунку** — внесення готівки, кратної 20 грн
- **Переказ коштів** — переказ на іншу картку за її номером
- **Зміна PIN-коду** — захищена зміна PIN після підтвердження поточного
- **Історія транзакцій** — пагінований перелік усіх операцій
- **Панель адміністратора** — створення рахунків, блокування/розблокування карток, пошук за ім'ям або номером картки
- **Middleware автентифікації** — захист усіх маршрутів; автоматичний вихід при блокуванні картки

---

## Технології

| Шар              | Технологія                   |
|------------------|------------------------------|
| Фреймворк        | ASP.NET Core MVC (.NET 8)    |
| ORM              | Entity Framework Core        |
| База даних       | MS SQL Server                |
| UI               | Razor Views (cshtml)         |
| Тестування       | xUnit                        |

---

## Запуск локально

### Вимоги
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- MS SQL Server або LocalDB

### Кроки

1. Клонувати репозиторій:
   ```bash
   git clone https://github.com/your-username/ATM.git
   cd ATM
   ```

2. Налаштувати рядок підключення в `AtmSimulator/appsettings.json`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=AtmSimulator;Trusted_Connection=True;"
   }
   ```

3. Застосувати міграції та заповнити базу початковими даними:
   ```bash
   cd AtmSimulator
   dotnet ef database update
   ```

4. Запустити застосунок:
   ```bash
   dotnet run
   ```

5. Відкрити у браузері: `https://localhost:5001`

### Тестові дані

| Номер картки       | PIN  | Власник     |
|--------------------|------|-------------|
| 1234567890123456   | 1234 | John Jones  |
| 6543210987654321   | 4321 | Will Smith  |

Панель адміністратора: `/Admin/Login` — пароль: `admin`

---

## Структура проєкту

```
AtmSimulator/
├── Controllers/          # MVC-контролери (Auth, Account, Transaction, Admin)
├── Data/                 # AppDbContext та початкові дані (seed)
├── Middleware/           # AuthMiddleware — захист маршрутів на основі сесії
├── Migrations/           # Міграції EF Core
├── Models/               # Доменні моделі (Account, Card, Transaction, CashStorage)
├── Patterns/
│   ├── Factory/          # TransactionFactory
│   ├── Observer/         # TransactionNotifier, TransactionLogger
│   └── Strategy/         # ICashDispenserStrategy, GreedyCashDispenserStrategy
├── Services/             # Бізнес-логіка (Auth, Withdrawal, Deposit, Transfer, Pin, Admin, Transaction)
├── ViewModels/           # DTO для передачі даних у представлення
└── Views/                # Razor-представлення (Account, Admin, Auth, Transaction)

AtmSimulator.Tests/
├── Patterns/             # Юніт-тести для патернів проєктування
└── Services/             # Юніт-тести для всіх сервісів
```

---

## Патерни проєктування

### 1. Strategy — ICashDispenserStrategy

**Файли:** [`Patterns/Strategy/ICashDispenserStrategy.cs`](AtmSimulator/Patterns/Strategy/ICashDispenserStrategy.cs), [`Patterns/Strategy/GreedyCashDispenserStrategy.cs`](AtmSimulator/Patterns/Strategy/GreedyCashDispenserStrategy.cs)

`WithdrawalService` залежить від інтерфейсу `ICashDispenserStrategy`, а не від конкретної реалізації. `GreedyCashDispenserStrategy` реалізує жадібний алгоритм, що обирає найбільші доступні номінали купюр для видачі. Завдяки цій абстракції можна замінити алгоритм розрахунку без жодних змін у сервісі.

```csharp
// Ін'єктується через DI — сервіс не знає про конкретну реалізацію
private readonly ICashDispenserStrategy _dispenserStrategy;
var dispensed = _dispenserStrategy.Calculate(amount, availableCash);
```

### 2. Observer — TransactionNotifier / ITransactionObserver

**Файли:** [`Patterns/Observer/ITransactionObserver.cs`](AtmSimulator/Patterns/Observer/ITransactionObserver.cs), [`Patterns/Observer/TransactionNotifier.cs`](AtmSimulator/Patterns/Observer/TransactionNotifier.cs), [`Patterns/Observer/TransactionLogger.cs`](AtmSimulator/Patterns/Observer/TransactionLogger.cs)

`TransactionNotifier` зберігає список підписників типу `ITransactionObserver` та сповіщає їх про кожну транзакцію. `TransactionLogger` — конкретний спостерігач, що записує структуровані логи через `ILogger`. Нові спостерігачі (наприклад, email-сповіщення або виявлення шахрайства) можна додати без змін у наявному коді.

```csharp
var notifier = new TransactionNotifier();
notifier.Subscribe(new TransactionLogger(logger));
await notifier.NotifyAsync(transaction);
```

### 3. Factory — TransactionFactory

**Файл:** [`Patterns/Factory/TransactionFactory.cs`](AtmSimulator/Patterns/Factory/TransactionFactory.cs)

`TransactionFactory.Create()` централізує створення об'єктів `Transaction` для всіх трьох типів операцій (зняття, поповнення, переказ). Кожен тип автоматично отримує правильний опис і часову мітку, що усуває дублювання у `WithdrawalService`, `DepositService` та `TransferService`.

```csharp
_context.Transactions.Add(
    TransactionFactory.Create(TransactionType.Withdrawal, accountId, amount, description)
);
```

---

## Техніки рефакторингу

1. **Extract Method** — бізнес-логіка кожної операції винесена в окремі сервісні класи (`WithdrawalService`, `DepositService` тощо) замість того, щоб знаходитись у контролерах.

2. **Replace Conditional with Polymorphism** — логіка підбору купюр замінила розгалужений умовний блок на інтерфейс `ICashDispenserStrategy` з ін'єктованою реалізацією.

3. **Introduce Parameter Object** — розрізнені примітивні параметри, що передавались у дії контролерів, згруповані у ViewModels (`WithdrawalViewModel`, `TransferViewModel`, `ChangePinViewModel` тощо).

4. **Separate Concerns via Middleware** — перевірки автентифікації та сесії винесені з контролерів у `AuthMiddleware`, щоб контролери зосереджувались виключно на своїх діях.

5. **Replace Magic Number with Named Constant** — максимальна кількість спроб введення PIN визначена як `private const int MaxPinAttempts = 3` у `AuthService` замість розкиданого по коду числа.

6. **Decompose Conditional** — логіка створення транзакцій розбита на окремі приватні методи `CreateWithdrawal`, `CreateDeposit` та `CreateTransfer` всередині `TransactionFactory` для кращої читабельності.

---

## Принципи програмування

### 1. Single Responsibility Principle (SRP)

Кожен клас має рівно одну причину для змін. `AuthService` відповідає лише за автентифікацію; `PinService` — лише за зміну PIN; `WithdrawalService` — лише за зняття готівки. Контролери делегують усю бізнес-логіку сервісам.

**Приклад:** [`Services/AuthService.cs`](AtmSimulator/Services/AuthService.cs)

### 2. Open/Closed Principle (OCP)

Інтерфейс `ICashDispenserStrategy` дозволяє додавати нові алгоритми видачі готівки без жодних змін у `WithdrawalService`.

**Приклад:** [`Patterns/Strategy/ICashDispenserStrategy.cs`](AtmSimulator/Patterns/Strategy/ICashDispenserStrategy.cs)

### 3. Dependency Inversion Principle (DIP)

Усі сервіси ін'єктуються через конструктори та реєструються в DI-контейнері в `Program.cs`. `WithdrawalService` залежить від абстракції `ICashDispenserStrategy`, а не від конкретного `GreedyCashDispenserStrategy`.

**Приклад:** [`Program.cs`](AtmSimulator/Program.cs), [`Services/WithdrawalService.cs`](AtmSimulator/Services/WithdrawalService.cs)

### 4. DRY (Don't Repeat Yourself)

`TransactionFactory` усуває дублювання створення об'єктів `Transaction`, яке інакше повторювалось би у трьох різних сервісах.

**Приклад:** [`Patterns/Factory/TransactionFactory.cs`](AtmSimulator/Patterns/Factory/TransactionFactory.cs)

### 5. Fail Fast

Сервіси негайно перевіряють передумови та кидають `InvalidOperationException` з описовим повідомленням ще до виконання будь-яких змін стану. Наприклад, `DepositService` перевіряє, що сума більша за 0 та кратна 20, до будь-яких змін балансу.

**Приклад:** [`Services/DepositService.cs`](AtmSimulator/Services/DepositService.cs), [`Services/PinService.cs`](AtmSimulator/Services/PinService.cs)

### 6. Separation of Concerns

Проєкт розділений на чітко відокремлені шари: Models (дані), Services (бізнес-логіка), Controllers (обробка HTTP), ViewModels (передача даних у UI), Middleware (наскрізні аспекти) та Patterns (структурні рішення).

---

## Тести

Юніт-тести знаходяться в `AtmSimulator.Tests/` та покривають:
- `AuthServiceTests` — пошук картки, валідація PIN, логіка блокування
- `WithdrawalServiceTests` — перевірка балансу, видача готівки
- `DepositServiceTests` — валідація суми, оновлення балансу
- `TransferServiceTests` — пошук відправника/отримувача, переказ балансу
- `PinServiceTests` — правила валідації PIN, процес зміни
- `TransactionServiceTests` — пагінація історії
- `GreedyCashDispenserStrategyTests` — алгоритм підбору купюр
- `TransactionFactoryTests` — коректне створення транзакцій кожного типу
- `TransactionNotifierTests` — сповіщення спостерігачів

```bash
cd AtmSimulator.Tests
dotnet test
```
