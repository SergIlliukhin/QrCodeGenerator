# QR Code Generator & Decoder

Цей проект складається з двох частин:
1. Генератор QR-кодів для шифрування сід-фраз
2. Декодер QR-кодів для розшифрування сід-фраз

## Генератор QR-кодів

Генератор створює зашифрований QR-код з вашої сід-фрази.

### Запуск генератора

```bash
dotnet run --project QrCodeGenerator.csproj
```

### Параметри для генератора
1. **Сід-фраза** - ваша секретна фраза, яку потрібно зашифрувати
2. **Пароль** - секретний пароль для шифрування (запам'ятайте його!)
3. **Назва файлу** - назва файлу для збереження QR-коду (необов'язково, за замовчуванням `seed_qr.png`)
   - Якщо не вказати розширення `.png`, воно буде додано автоматично
   - Натисніть Enter для використання назви за замовчуванням

Результат: QR-код буде збережено у вказаний файл у поточній директорії.

## Декодер QR-кодів

Декодер розшифровує QR-код та відновлює оригінальну сід-фразу.

### Запуск декодера

```bash
dotnet run --project QrCodeDecryptor.csproj
```

### Параметри для декодера
1. **Шлях до QR-коду** - шлях до файлу з QR-кодом (наприклад, `seed_qr.png`)
2. **Пароль** - той самий пароль, який використовувався при шифруванні

## Можливі помилки

### Генератор
- ❌ Помилка при створенні QR-коду: перевірте, чи є права на запис у поточній директорії

### Декодер
- ❌ QR-код не знайдено: перевірте правильність шляху до файлу
- ❌ Неможливо відкрити файл як зображення: перевірте, чи файл не пошкоджений
- ❌ QR-код не розпізнано: перевірте, чи файл містить валідний QR-код
- ❌ Невірний пароль: введіть правильний пароль, який використовувався при шифруванні

## Системні вимоги
- .NET 9.0
- Підтримуються всі операційні системи (Windows, macOS, Linux)