using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddedSaleInvoiceBuyInvoiceSaleInvoiceItemBuyInvoiceItemCustomerPaymentSupplierPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "buy_invoice",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    number = table.Column<long>(type: "bigint", nullable: false),
                    type = table.Column<int>(type: "int", nullable: false),
                    date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    notes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    supplier_id = table.Column<long>(type: "bigint", nullable: false),
                    auditor_id = table.Column<long>(type: "bigint", nullable: false),
                    order_id = table.Column<long>(type: "bigint", nullable: true),
                    original_invoice_id = table.Column<long>(type: "bigint", nullable: true),
                    total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    discount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    net_total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    net_total_currency = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_buy_invoice", x => x.id);
                    table.ForeignKey(
                        name: "FK_buy_invoice_buy_invoice_original_invoice_id",
                        column: x => x.original_invoice_id,
                        principalTable: "buy_invoice",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_buy_invoice_employee_auditor_id",
                        column: x => x.auditor_id,
                        principalTable: "employee",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_buy_invoice_order_order_id",
                        column: x => x.order_id,
                        principalTable: "order",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_buy_invoice_supplier_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "supplier",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "feedback_question",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_feedback_question", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sale_invoice",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    number = table.Column<long>(type: "bigint", nullable: false),
                    type = table.Column<int>(type: "int", nullable: false),
                    date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    notes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    customer_id = table.Column<long>(type: "bigint", nullable: false),
                    auditor_id = table.Column<long>(type: "bigint", nullable: false),
                    order_id = table.Column<long>(type: "bigint", nullable: true),
                    original_invoice_id = table.Column<long>(type: "bigint", nullable: true),
                    total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    discount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    net_total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    net_total_currency = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sale_invoice", x => x.id);
                    table.ForeignKey(
                        name: "FK_sale_invoice_customer_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customer",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sale_invoice_employee_auditor_id",
                        column: x => x.auditor_id,
                        principalTable: "employee",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sale_invoice_order_order_id",
                        column: x => x.order_id,
                        principalTable: "order",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_sale_invoice_sale_invoice_original_invoice_id",
                        column: x => x.original_invoice_id,
                        principalTable: "sale_invoice",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "buy_invoice_item",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    invoice_id = table.Column<long>(type: "bigint", nullable: false),
                    product_id = table.Column<long>(type: "bigint", nullable: false),
                    store_id = table.Column<long>(type: "bigint", nullable: false),
                    project_id = table.Column<long>(type: "bigint", nullable: true),
                    product_notes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    price_type = table.Column<int>(type: "int", nullable: false),
                    product_price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    product_quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    product_total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    product_discount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    product_net_total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_buy_invoice_item", x => x.id);
                    table.ForeignKey(
                        name: "FK_buy_invoice_item_buy_invoice_invoice_id",
                        column: x => x.invoice_id,
                        principalTable: "buy_invoice",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_buy_invoice_item_product_product_id",
                        column: x => x.product_id,
                        principalTable: "product",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_buy_invoice_item_project_project_id",
                        column: x => x.project_id,
                        principalTable: "project",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_buy_invoice_item_store_store_id",
                        column: x => x.store_id,
                        principalTable: "store",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "supplier_payment",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    supplier_id = table.Column<long>(type: "bigint", nullable: false),
                    buy_invoice_id = table.Column<long>(type: "bigint", nullable: true),
                    money_safe_id = table.Column<long>(type: "bigint", nullable: false),
                    amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    amount_currency = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    notes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    auditor_id = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_supplier_payment", x => x.id);
                    table.ForeignKey(
                        name: "FK_supplier_payment_buy_invoice_buy_invoice_id",
                        column: x => x.buy_invoice_id,
                        principalTable: "buy_invoice",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_supplier_payment_employee_auditor_id",
                        column: x => x.auditor_id,
                        principalTable: "employee",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_payment_money_safe_money_safe_id",
                        column: x => x.money_safe_id,
                        principalTable: "money_safe",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_payment_supplier_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "supplier",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "customer_payment",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    customer_id = table.Column<long>(type: "bigint", nullable: false),
                    sale_invoice_id = table.Column<long>(type: "bigint", nullable: true),
                    money_safe_id = table.Column<long>(type: "bigint", nullable: false),
                    amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    amount_currency = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    notes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    auditor_id = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_payment", x => x.id);
                    table.ForeignKey(
                        name: "FK_customer_payment_customer_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customer",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_customer_payment_employee_auditor_id",
                        column: x => x.auditor_id,
                        principalTable: "employee",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_customer_payment_money_safe_money_safe_id",
                        column: x => x.money_safe_id,
                        principalTable: "money_safe",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_customer_payment_sale_invoice_sale_invoice_id",
                        column: x => x.sale_invoice_id,
                        principalTable: "sale_invoice",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "sale_invoice_item",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    invoice_id = table.Column<long>(type: "bigint", nullable: false),
                    product_id = table.Column<long>(type: "bigint", nullable: false),
                    store_id = table.Column<long>(type: "bigint", nullable: false),
                    project_id = table.Column<long>(type: "bigint", nullable: true),
                    product_notes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    price_type = table.Column<int>(type: "int", nullable: false),
                    product_price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    product_quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    product_total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    product_discount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    product_net_total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sale_invoice_item", x => x.id);
                    table.ForeignKey(
                        name: "FK_sale_invoice_item_product_product_id",
                        column: x => x.product_id,
                        principalTable: "product",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sale_invoice_item_project_project_id",
                        column: x => x.project_id,
                        principalTable: "project",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_sale_invoice_item_sale_invoice_invoice_id",
                        column: x => x.invoice_id,
                        principalTable: "sale_invoice",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_sale_invoice_item_store_store_id",
                        column: x => x.store_id,
                        principalTable: "store",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_buy_invoice_auditor_id",
                table: "buy_invoice",
                column: "auditor_id");

            migrationBuilder.CreateIndex(
                name: "IX_buy_invoice_order_id",
                table: "buy_invoice",
                column: "order_id",
                unique: true,
                filter: "[order_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_buy_invoice_original_invoice_id",
                table: "buy_invoice",
                column: "original_invoice_id");

            migrationBuilder.CreateIndex(
                name: "IX_buy_invoice_supplier_id",
                table: "buy_invoice",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "IX_buy_invoice_item_invoice_id",
                table: "buy_invoice_item",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "IX_buy_invoice_item_product_id",
                table: "buy_invoice_item",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_buy_invoice_item_project_id",
                table: "buy_invoice_item",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "IX_buy_invoice_item_store_id",
                table: "buy_invoice_item",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_payment_auditor_id",
                table: "customer_payment",
                column: "auditor_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_payment_customer_id",
                table: "customer_payment",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_payment_money_safe_id",
                table: "customer_payment",
                column: "money_safe_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_payment_sale_invoice_id",
                table: "customer_payment",
                column: "sale_invoice_id");

            migrationBuilder.CreateIndex(
                name: "IX_sale_invoice_auditor_id",
                table: "sale_invoice",
                column: "auditor_id");

            migrationBuilder.CreateIndex(
                name: "IX_sale_invoice_customer_id",
                table: "sale_invoice",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_sale_invoice_order_id",
                table: "sale_invoice",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_sale_invoice_original_invoice_id",
                table: "sale_invoice",
                column: "original_invoice_id");

            migrationBuilder.CreateIndex(
                name: "IX_sale_invoice_item_invoice_id",
                table: "sale_invoice_item",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "IX_sale_invoice_item_product_id",
                table: "sale_invoice_item",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_sale_invoice_item_project_id",
                table: "sale_invoice_item",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "IX_sale_invoice_item_store_id",
                table: "sale_invoice_item",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_payment_auditor_id",
                table: "supplier_payment",
                column: "auditor_id");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_payment_buy_invoice_id",
                table: "supplier_payment",
                column: "buy_invoice_id");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_payment_money_safe_id",
                table: "supplier_payment",
                column: "money_safe_id");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_payment_supplier_id",
                table: "supplier_payment",
                column: "supplier_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "buy_invoice_item");

            migrationBuilder.DropTable(
                name: "customer_payment");

            migrationBuilder.DropTable(
                name: "feedback_question");

            migrationBuilder.DropTable(
                name: "sale_invoice_item");

            migrationBuilder.DropTable(
                name: "supplier_payment");

            migrationBuilder.DropTable(
                name: "sale_invoice");

            migrationBuilder.DropTable(
                name: "buy_invoice");
        }
    }
}
