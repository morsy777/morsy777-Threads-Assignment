## Running the phases

Run commands from this project directory (`06-OrderProcessing\OrderProcessing`):

```powershell
dotnet build .\OrderProcessing.csproj
dotnet run --project .\OrderProcessing.csproj
```

`Program.cs` is the entry point. The phase runner calls are intentionally left commented so one phase can be selected at a time. Uncomment the call for the phase you want, and keep the other phase calls commented; then run the command above. The runner classes can also be invoked from another C# entry point.

| Phase | Runner to enable in `Program.cs` | What it runs |
|---|---|---|
| 1 | `var runner = new Phase1Runner(); await runner.RunAsync();` | Seeds input data and compares synchronous and asynchronous order validation. `LogSummary()` can be called afterward for the validation summary. |
| 2 | `var runner = new Phase2Runner(); await runner.RunAsync();` | Runs the race-condition demonstration, pricing performance checks, and the asynchronous order-processing/pricing exercises. |
| 3 | `var runner = new Phase3Runner(); await runner.RunAsync();` | Compares asynchronous order processing with the synchronous/thread-based version. |
| 4 | `var runner = new Phase4Runner(); await runner.RunAsync();` | Runs live checkout, customer dashboard loading, fastest shipping quotes, and CPU/I/O parallel-work demonstrations. Individual methods can be enabled instead when only one Phase 4 task is needed. |

## Phase screenshots

### Phase 1

![Phase 1](./Screetshots/Phase1.png)

### Phase 2

![Phase 2 - 1](./Screetshots/Phase2-img1.png)

![Phase 2 - 2](./Screetshots/Phase2-img2.png)

![Phase 2 - 3](./Screetshots/Phase2-img3.png)

![Phase 2 - 4](./Screetshots/Phase2-img4.png)

### Phase 3

![Phase 3 - 1](./Screetshots/Phase3-img1.png)

![Phase 3 - 2](./Screetshots/Phase3-img2.png)

![Phase 3 - 3](./Screetshots/Phase3-img3.png)

![Phase 3 - ProcessOrderAsync - 1](./Screetshots/Phase3-ProcessOrderAsync-img1.png)

![Phase 3 - ProcessOrderAsync - 2](./Screetshots/Phase3-ProcessOrderAsync-img2.png)

![Phase 3 - ProcessOrderAsync - 3](./Screetshots/Phase3-ProcessOrderAsync-img3.png)

![Phase 3 - ProcessOrderAsync - 4](./Screetshots/Phase3-ProcessOrderAsync-img4.png)

![Phase 3 - ProcessOrderAsync - 5](./Screetshots/Phase3-ProcessOrderAsync-img5.png)

![Phase 3 - Async vs Sync Order Processing - 1](./Screetshots/Phase3-Async-vs-Sync-OrderProcessing-img1.png)

![Phase 3 - Async vs Sync Order Processing - 2](./Screetshots/Phase3-Async-vs-Sync-OrderProcessing-img2.png)

### Phase 4

![Phase 4 - 10.1 Live Checkout Progress](./Screetshots/Phase4-10.1-liveCheckoutProgress.png)

![Phase 4 - Cancel After 2 Seconds - 1](./Screetshots/Phase4-cancelAfter2sec-img1.png)

![Phase 4 - Cancel After 2 Seconds - 2](./Screetshots/Phase4-cancelAfter2sec-img2.png)

![Phase 4 - 10.2 Customer Dashboard Service](./Screetshots/Phase4-10.2-CustomerDashboardService.png)

![Phase 4 - 10.3 Shipping Service - 1](./Screetshots/Phase4-10.3-ShippingService.png)

![Phase 4 - 10.3 Shipping Service - 2](./Screetshots/Phase4-10.3-ShippingService2.png)

![Phase 4 - 10.3 I/O Bound](./Screetshots/Phase4-10.3-IoBound.png)

![Phase 4 - 10.4 CPU Bound](./Screetshots/Phase4-10.4-CpuBound.png)

The `Data` directory is created under `AppContext.BaseDirectory`, which is the executable output directory, not the shell's current directory. For a normal Debug run this is:

```text
OrderProcessing\bin\Debug\net10.0\Data\
```

The corresponding `Release` run uses `bin\Release\net10.0\Data\`. The seeder resets this directory before seeding, so runner workflows that seed data replace the previous data in that output directory.

## Project structure and class responsibilities

| Folder / class | Responsibility |
|---|---|
| `Program.cs` | Top-level application entry point; select and invoke a phase runner. |
| `Logger.cs` | Shared console logging helpers. |
| `Runners\Phase1Runner` | Seed data, execute validation comparisons, and print validation results. |
| `Runners\Phase2Runner` | Coordinate Phase 2 pricing, race-condition, and order-processing demonstrations. |
| `Runners\Phase3Runner` | Compare async and synchronous processing of incoming orders. |
| `Runners\Phase4Runner` | Coordinate live checkout, dashboards, shipping quotes, CPU scoring, and email resend work; report progress/results. |
| `Runners\ControlledFailuresRunner` | Run configured failure scenarios and check expected outcomes. |
| `Runners\RaceConditionDemo` | Demonstrate unsafe concurrent stock reservation. |
| `Phase4Runner.OrderExecutionResult` | Hold an order and its elapsed processing time for Phase 4 progress and summary output. |
| `Models\Order` | Order data, status, totals, failure reason, and transaction reference. |
| `Models\OrderItem` | Product, quantity, and unit price for an order line. |
| `Models\Product` | Product data and available stock. |
| `Models\Customer` | Customer profile, active state, loyalty tier, and email generation. |
| `Models\Coupon` | Coupon code, discount, and expiration date. |
| `Models\PaymentTransaction` | Payment amount, status, transaction ID, and order reference. |
| `Models\EmailMessage` | Sent or retry-queued email information. |
| `Models\CustomerDashboard` | Combined customer, order, payment, and email dashboard data. |
| `Models\ShippingQuote` | Carrier quote, price, elapsed time, and cancelled-carrier list. |
| `Models\Report` | Aggregated order/revenue/product report data. |
| `Models\Enums\OrderStatus` | Order lifecycle status values. |
| `Models\Enums\PaymentStatus` | Payment lifecycle status values. |
| `Models\Enums\LoyaltyTier` | Customer loyalty levels used in pricing. |
| `Repositories\OrderRepository` | Read and update order records. |
| `Repositories\IncomingOrderRepository` | Load seeded incoming orders. |
| `Repositories\ProductRepository` | Read products and reserve/release stock. |
| `Repositories\CustomerRepository` | Load customer records. |
| `Repositories\CouponRepository` | Load and check coupon records. |
| `Services\OrderProcessor` | Execute the Phase 3/4 async order workflow, including payment, persistence, email, and compensation. |
| `Services\OrderProcessorV2` | Execute the earlier async order-processing/pricing workflow. |
| `Services\CheckoutService` | Run the checkout workflow with validation, payment, invoice, and email steps. |
| `Services\CustomerService` | Retrieve customers for service workflows. |
| `Services\InventoryService` | Coordinate stock reservation and release for an order. |
| `Services\PricingService` | Calculate an order's subtotal, discount, shipping, and total. |
| `Services\OrderPricer` | Calculate item pricing, discounts, shipping, and pricing demonstrations. |
| `Services\PaymentGateway` | Simulate payment capture, decline, timeout, and refund operations. |
| `Services\IPaymentGateway` | Async payment-gateway contract. |
| `Services\EmailService` | Send confirmation email messages and simulate SMTP failures. |
| `Services\IEmailService` | Async email-service contract. |
| `Services\CustomerDashboardService` | Load customer, order, payment, and email dashboard data concurrently. |
| `Services\ShippingQuoteService` | Request carrier quotes, select the fastest successful quote, and cancel losing requests. |
| `Services\BaseShippingCarrier` | Shared quote/delay/failure behavior for shipping carriers. |
| `Services\IShippingCarrier` | Shipping-carrier contract. |
| `Services\CairoExpress`, `Services\FastShip`, `Services\NileCourier` | Individual shipping carrier implementations. |
| `Services\InvoiceGenerator` | Generate a text invoice for an order. |
| `Services\ReportGenerator` | Aggregate stored order and product data into a report. |
| `Validators\OrderValidator` | Validate orders asynchronously and set validation status/reason. |
| `SyncValidators\SyncOrderValidator` | Synchronous counterpart to order validation. |
| `SyncServices\SyncOrderProcessor` | Synchronous order-processing workflow. |
| `SyncServices\SyncOrderPricer` | Synchronous pricing operations. |
| `SyncServices\SyncPricingService` | Synchronous pricing workflow. |
| `SyncServices\SyncInventoryService` | Synchronous inventory operations. |
| `SyncServices\SyncCustomerService` | Synchronous customer lookup. |
| `SyncServices\SyncPaymentGateway` | Synchronous payment capture/refund simulation. |
| `SyncServices\ISyncPaymentGateway` | Synchronous payment-gateway contract. |
| `SyncServices\SyncEmailService` | Synchronous email operation. |
| `SyncServices\ISyncEmailService` | Synchronous email-service contract. |
| `SyncRepositories\SyncOrderRepository` | Synchronous order persistence. |
| `SyncRepositories\SyncProductRepository` | Synchronous product and stock persistence. |
| `SyncRepositories\SyncCustomerRepository` | Synchronous customer lookup. |
| `SyncRepositories\SyncCouponRepository` | Synchronous coupon lookup. |
| `Persistence\JsonFileStore` | Shared JSON file read/write implementation and per-file async locking. |
| `Persistence\JsonSerializerConfig` | Shared JSON naming, formatting, and enum-serialization settings. |
| `Persistence\DataSeeder` | Reset and seed customers, products, coupons, and incoming orders. |
| `Persistence\Constants\DataFiles` | Central names for data files and generated-output directories. |
| `Results\PaymentResult` | Payment operation result returned to the order workflow. |
| `Exceptions\CustomerNotFoundException` | Indicates a customer lookup failed. |
| `Exceptions\InvalidCouponException` | Indicates an invalid or expired coupon. |
| `Exceptions\DatabaseWriteException` | Indicates a simulated order persistence failure. |
| `Exceptions\DataFileCorruptedException` | Indicates invalid JSON data. |
| `Exceptions\ShippingProviderUnavailableException` | Indicates no usable shipping provider quote. |
| `Exceptions\SmtpUnavailableException` | Indicates an email delivery failure. |

## Storage design and concurrent access

The application stores JSON arrays of model objects in `Data`. `JsonFileStore` uses indented JSON, camelCase property names, and string values for enums. The persisted file names are centralized in `Persistence\Constants\DataFiles`.

| File / directory | Contents |
|---|---|
| `customers.json` | Customer records (`Customer`). |
| `products.json` | Product records and current stock (`Product`). |
| `coupons.json` | Coupon records (`Coupon`). |
| `incoming_orders.json` | Seeded orders awaiting processing (`Order`). |
| `orders.json` | Processed/validated order records (`Order`). |
| `payments.json` | Payment transactions (`PaymentTransaction`). |
| `emails.json` | Sent confirmation messages (`EmailMessage`). |
| `email_retry.json` | Email messages queued for retry (`EmailMessage`). |
| `invoices\invoice_{orderId}.txt` | Generated text invoices; created by `InvoiceGenerator`. |
| `reports\report_{timestamp}.json` | Generated summary reports; created by `ReportGenerator`. |

The entity-store files (`customers.json`, `products.json`, `coupons.json`, `incoming_orders.json`, `orders.json`, `payments.json`, `emails.json`, and `email_retry.json`) contain JSON arrays. For example, `orders.json` is an array of objects with camelCase properties such as `id`, `customerId`, `items`, `status`, and `transactionId`; enum properties such as `status` are serialized as readable strings (for example, `"Completed"`). Dates use the `System.Text.Json` date/time representation. Each `report_{timestamp}.json` file contains one report object.

`JsonFileStore` keeps a static `ConcurrentDictionary` of `SemaphoreSlim` locks keyed by each file's normalized full path. A read or write waits for that file's lock and releases it in `finally`, so calls through the store cannot read a file while another store call is writing it. Different files have separate locks and can be accessed concurrently. For writes, it serializes to a temporary file and replaces the destination with `File.Move(..., overwrite: true)`, avoiding exposure of a partially written JSON document.

Repositories also coordinate their workflows: `OrderRepository` serializes order saves, and `ProductRepository` serializes stock reservation/release operations through its stock semaphore. The product semaphore is an instance member, so it coordinates calls that share that repository instance; the per-file `JsonFileStore` lock protects each file operation regardless. These locks are in-process safeguards, not cross-process locks or a multi-file transaction. A complete read-modify-write sequence across separate repository instances is not made atomic by the file-store lock alone.


