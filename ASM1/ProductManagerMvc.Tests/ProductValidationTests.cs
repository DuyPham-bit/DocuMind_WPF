using System.ComponentModel.DataAnnotations;
using ProductManagerMvc.Models;

namespace ProductManagerMvc.Tests;

public class ProductValidationTests
{
    private static List<ValidationResult> ValidateModel(Product product)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(product);
        Validator.TryValidateObject(product, context, results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void Product_WithValidProperties_PassesValidation()
    {
        var product = new Product
        {
            Name = "Chuột gaming",
            Description = "Chuột quang không dây 16000 DPI",
            Price = 250000,
            Stock = 12
        };

        var errors = ValidateModel(product);

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Product_WithEmptyName_FailsValidation(string invalidName)
    {
        var product = new Product
        {
            Name = invalidName,
            Price = 250000,
            Stock = 5
        };

        var errors = ValidateModel(product);

        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Product.Name)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Product_WithZeroOrNegativePrice_FailsValidation(decimal invalidPrice)
    {
        var product = new Product
        {
            Name = "Bàn phím cơ",
            Price = invalidPrice,
            Stock = 7
        };

        var errors = ValidateModel(product);

        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Product.Price)));
    }

    [Fact]
    public void Product_WithNegativeStock_FailsValidation()
    {
        var product = new Product
        {
            Name = "Tai nghe USB",
            Price = 320000,
            Stock = -1
        };

        var errors = ValidateModel(product);

        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(Product.Stock)));
    }
}
