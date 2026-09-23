using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Syncronex.Api.Features.Webhooks;

// =========================================================================
// 1. INGESTION MODELS (Input from Shopify Simulator)
// =========================================================================

public record ShopifyWebhookPayload(
    [property: JsonPropertyName("event_id")] string EventId,
    [property: JsonPropertyName("event_type")] string EventType,
    [property: JsonPropertyName("timestamp")] DateTime Timestamp,
    [property: JsonPropertyName("data")] ShopifyOrderData Data
);

public record ShopifyOrderData(
    [property: JsonPropertyName("order_id")] string OrderId,
    [property: JsonPropertyName("currency")] string Currency,
    [property: JsonPropertyName("total_amount")] decimal TotalAmount,
    [property: JsonPropertyName("customer")] ShopifyCustomer Customer,
    [property: JsonPropertyName("line_items")] IReadOnlyList<ShopifyLineItem> LineItems
);

public record ShopifyCustomer(
    [property: JsonPropertyName("customer_id")] string CustomerId,
    [property: JsonPropertyName("first_name")] string FirstName,
    [property: JsonPropertyName("last_name")] string LastName,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("phone")] string Phone
);

public record ShopifyLineItem(
    [property: JsonPropertyName("sku")] string Sku,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("quantity")] int Quantity,
    [property: JsonPropertyName("unit_price")] decimal UnitPrice
);

// =========================================================================
// 2. EXTERNAL SYSTEM MODELS (Mapped Outputs)
// =========================================================================

#region CRM Integration Models (HubSpot / Salesforce)

public record CrmUpsertPayload(
    [property: JsonPropertyName("contact")] CrmContact Contact,
    [property: JsonPropertyName("recent_transaction")] CrmTransaction RecentTransaction
);

public record CrmContact(
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("first_name")] string FirstName,
    [property: JsonPropertyName("last_name")] string LastName,
    [property: JsonPropertyName("phone")] string Phone
);

public record CrmTransaction(
    [property: JsonPropertyName("source_order_id")] string SourceOrderId,
    [property: JsonPropertyName("amount")] decimal Amount,
    [property: JsonPropertyName("currency")] string Currency,
    [property: JsonPropertyName("purchased_at")] DateTime PurchasedAt
);

#endregion

#region Accounting Integration Models (QuickBooks / Xero)

public record AccountingInvoicePayload(
    [property: JsonPropertyName("invoice_reference")] string InvoiceReference,
    [property: JsonPropertyName("customer_reference")] string CustomerReference,
    [property: JsonPropertyName("issue_date")] string IssueDate, // e.g., "yyyy-MM-dd"
    [property: JsonPropertyName("currency")] string Currency,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("total_amount")] decimal TotalAmount,
    [property: JsonPropertyName("line_items")] IReadOnlyList<AccountingLineItem> LineItems
);

public record AccountingLineItem(
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("amount")] decimal Amount,
    [property: JsonPropertyName("tax_code")] string TaxCode
);

#endregion
