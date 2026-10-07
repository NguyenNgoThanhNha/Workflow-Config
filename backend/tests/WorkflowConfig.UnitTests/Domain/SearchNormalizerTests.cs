using WorkflowConfig.Domain.Common;

namespace WorkflowConfig.UnitTests.Domain;

public class SearchNormalizerTests
{
    [Theory]
    [InlineData("Lỗi xuất HÓA ĐƠN điện tử", "loi xuat hoa don dien tu")]
    [InlineData("  Đăng   nhập\tthất bại ", "dang nhap that bai")]
    [InlineData("VPN không kết nối", "vpn khong ket noi")]
    [InlineData(null, "")]
    [InlineData("   ", "")]
    public void Lowercases_strips_vietnamese_diacritics_and_collapses_spaces(string? input, string expected) =>
        Assert.Equal(expected, SearchNormalizer.Normalize(input));

    [Fact]
    public void Keyword_with_or_without_accents_normalizes_the_same() =>
        Assert.Equal(SearchNormalizer.Normalize("hoa don"), SearchNormalizer.Normalize("Hóa Đơn"));
}
