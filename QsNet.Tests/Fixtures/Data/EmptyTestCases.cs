using System.Collections.Generic;

namespace QsNet.Tests.Fixtures.Data;

internal static class EmptyTestCases
{
    public static readonly List<Dictionary<string, object?>> Cases =
    [
        new()
        {
            ["input"] = "&",
            ["withEmptyKeys"] = new Dictionary<string, object?>(),
            ["stringifyOutput"] = new Dictionary<string, object?>
            {
                ["brackets"] = "",
                ["indices"] = "",
                ["repeat"] = ""
            },
            ["noEmptyKeys"] = new Dictionary<string, object?>()
        },
        new()
        {
            ["input"] = "&&",
            ["withEmptyKeys"] = new Dictionary<string, object?>(),
            ["stringifyOutput"] = new Dictionary<string, object?>
            {
                ["brackets"] = "",
                ["indices"] = "",
                ["repeat"] = ""
            },
            ["noEmptyKeys"] = new Dictionary<string, object?>()
        },
        new()
        {
            ["input"] = "&=",
            ["withEmptyKeys"] = new Dictionary<string, object?> { [""] = "" },
            ["stringifyOutput"] = new Dictionary<string, object?>
            {
                ["brackets"] = "=",
                ["indices"] = "=",
                ["repeat"] = "="
            },
            ["noEmptyKeys"] = new Dictionary<string, object?>()
        },
        new()
        {
            ["input"] = "&=&",
            ["withEmptyKeys"] = new Dictionary<string, object?> { [""] = "" },
            ["stringifyOutput"] = new Dictionary<string, object?>
            {
                ["brackets"] = "=",
                ["indices"] = "=",
                ["repeat"] = "="
            },
            ["noEmptyKeys"] = new Dictionary<string, object?>()
        },
        new()
        {
            ["input"] = "&=&=",
            ["withEmptyKeys"] = new Dictionary<string, object?>
            {
                [""] = new List<object?> { "", "" }
            },
            ["stringifyOutput"] = new Dictionary<string, object?>
            {
                ["brackets"] = "[]=&[]=",
                ["indices"] = "[0]=&[1]=",
                ["repeat"] = "=&="
            },
            ["noEmptyKeys"] = new Dictionary<string, object?>()
        },
        new()
        {
            ["input"] = "&=&=&",
            ["withEmptyKeys"] = new Dictionary<string, object?>
            {
                [""] = new List<object?> { "", "" }
            },
            ["stringifyOutput"] = new Dictionary<string, object?>
            {
                ["brackets"] = "[]=&[]=",
                ["indices"] = "[0]=&[1]=",
                ["repeat"] = "=&="
            },
            ["noEmptyKeys"] = new Dictionary<string, object?>()
        },
        new()
        {
            ["input"] = "=",
            ["withEmptyKeys"] = new Dictionary<string, object?> { [""] = "" },
            ["noEmptyKeys"] = new Dictionary<string, object?>(),
            ["stringifyOutput"] = new Dictionary<string, object?>
            {
                ["brackets"] = "=",
                ["indices"] = "=",
                ["repeat"] = "="
            }
        },
        new()
        {
            ["input"] = "=&",
            ["withEmptyKeys"] = new Dictionary<string, object?> { [""] = "" },
            ["stringifyOutput"] = new Dictionary<string, object?>
            {
                ["brackets"] = "=",
                ["indices"] = "=",
                ["repeat"] = "="
            },
            ["noEmptyKeys"] = new Dictionary<string, object?>()
        },
        new()
        {
            ["input"] = "=&&&",
            ["withEmptyKeys"] = new Dictionary<string, object?> { [""] = "" },
            ["stringifyOutput"] = new Dictionary<string, object?>
            {
                ["brackets"] = "=",
                ["indices"] = "=",
                ["repeat"] = "="
            },
            ["noEmptyKeys"] = new Dictionary<string, object?>()
        },
        new()
        {
            ["input"] = "=&=&=&",
            ["withEmptyKeys"] = new Dictionary<string, object?>
            {
                [""] = new List<object?> { "", "", "" }
            },
            ["stringifyOutput"] = new Dictionary<string, object?>
            {
                ["brackets"] = "[]=&[]=&[]=",
                ["indices"] = "[0]=&[1]=&[2]=",
                ["repeat"] = "=&=&="
            },
            ["noEmptyKeys"] = new Dictionary<string, object?>()
        },
        new()
        {
            ["input"] = "=&a[]=b&a[1]=c",
            ["withEmptyKeys"] = new Dictionary<string, object?>
            {
                [""] = "",
                ["a"] = new List<object?> { "b", "c" }
            },
            ["stringifyOutput"] = new Dictionary<string, object?>
            {
                ["brackets"] = "=&a[]=b&a[]=c",
                ["indices"] = "=&a[0]=b&a[1]=c",
                ["repeat"] = "=&a=b&a=c"
            },
            ["noEmptyKeys"] = new Dictionary<string, object?>
            {
                ["a"] = new List<object?> { "b", "c" }
            }
        },
        new()
        {
            ["input"] = "=a",
            ["withEmptyKeys"] = new Dictionary<string, object?> { [""] = "a" },
            ["noEmptyKeys"] = new Dictionary<string, object?>(),
            ["stringifyOutput"] = new Dictionary<string, object?>
            {
                ["brackets"] = "=a",
                ["indices"] = "=a",
                ["repeat"] = "=a"
            }
        },
        new()
        {
            ["input"] = "a==a",
            ["withEmptyKeys"] = new Dictionary<string, object?> { ["a"] = "=a" },
            ["noEmptyKeys"] = new Dictionary<string, object?> { ["a"] = "=a" },
            ["stringifyOutput"] = new Dictionary<string, object?>
            {
                ["brackets"] = "a==a",
                ["indices"] = "a==a",
                ["repeat"] = "a==a"
            }
        },
        new()
        {
            ["input"] = "=&a[]=b",
            ["withEmptyKeys"] = new Dictionary<string, object?>
            {
                [""] = "",
                ["a"] = new List<object?> { "b" }
            },
            ["stringifyOutput"] = new Dictionary<string, object?>
            {
                ["brackets"] = "=&a[]=b",
                ["indices"] = "=&a[0]=b",
                ["repeat"] = "=&a=b"
            },
            ["noEmptyKeys"] = new Dictionary<string, object?> { ["a"] = new List<object?> { "b" } }
        },
        new()
        {
            ["input"] = "=&a[]=b&a[]=c&a[2]=d",
            ["withEmptyKeys"] = new Dictionary<string, object?>
            {
                [""] = "",
                ["a"] = new List<object?> { "b", "c", "d" }
            },
            ["stringifyOutput"] = new Dictionary<string, object?>
            {
                ["brackets"] = "=&a[]=b&a[]=c&a[]=d",
                ["indices"] = "=&a[0]=b&a[1]=c&a[2]=d",
                ["repeat"] = "=&a=b&a=c&a=d"
            },
            ["noEmptyKeys"] = new Dictionary<string, object?>
            {
                ["a"] = new List<object?> { "b", "c", "d" }
            }
        },
        new()
        {
            ["input"] = "=a&=b",
            ["withEmptyKeys"] = new Dictionary<string, object?>
            {
                [""] = new List<object?> { "a", "b" }
            },
            ["stringifyOutput"] = new Dictionary<string, object?>
            {
                ["brackets"] = "[]=a&[]=b",
                ["indices"] = "[0]=a&[1]=b",
                ["repeat"] = "=a&=b"
            },
            ["noEmptyKeys"] = new Dictionary<string, object?>()
        },
        new()
        {
            ["input"] = "=a&foo=b",
            ["withEmptyKeys"] = new Dictionary<string, object?> { [""] = "a", ["foo"] = "b" },
            ["noEmptyKeys"] = new Dictionary<string, object?> { ["foo"] = "b" },
            ["stringifyOutput"] = new Dictionary<string, object?>
            {
                ["brackets"] = "=a&foo=b",
                ["indices"] = "=a&foo=b",
                ["repeat"] = "=a&foo=b"
            }
        },
        new()
        {
            ["input"] = "a[]=b&a=c&=",
            ["withEmptyKeys"] = new Dictionary<string, object?>
            {
                [""] = "",
                ["a"] = new List<object?> { "b", "c" }
            },
            ["stringifyOutput"] = new Dictionary<string, object?>
            {
                ["brackets"] = "=&a[]=b&a[]=c",
                ["indices"] = "=&a[0]=b&a[1]=c",
                ["repeat"] = "=&a=b&a=c"
            },
            ["noEmptyKeys"] = new Dictionary<string, object?>
            {
                ["a"] = new List<object?> { "b", "c" }
            }
        },
        new()
        {
            ["input"] = "a[0]=b&a=c&=",
            ["withEmptyKeys"] = new Dictionary<string, object?>
            {
                [""] = "",
                ["a"] = new List<object?> { "b", "c" }
            },
            ["stringifyOutput"] = new Dictionary<string, object?>
            {
                ["brackets"] = "=&a[]=b&a[]=c",
                ["indices"] = "=&a[0]=b&a[1]=c",
                ["repeat"] = "=&a=b&a=c"
            },
            ["noEmptyKeys"] = new Dictionary<string, object?>
            {
                ["a"] = new List<object?> { "b", "c" }
            }
        },
        new()
        {
            ["input"] = "a=b&a[]=c&=",
            ["withEmptyKeys"] = new Dictionary<string, object?>
            {
                [""] = "",
                ["a"] = new List<object?> { "b", "c" }
            },
            ["stringifyOutput"] = new Dictionary<string, object?>
            {
                ["brackets"] = "=&a[]=b&a[]=c",
                ["indices"] = "=&a[0]=b&a[1]=c",
                ["repeat"] = "=&a=b&a=c"
            },
            ["noEmptyKeys"] = new Dictionary<string, object?>
            {
                ["a"] = new List<object?> { "b", "c" }
            }
        },
        new()
        {
            ["input"] = "a=b&a[0]=c&=",
            ["withEmptyKeys"] = new Dictionary<string, object?>
            {
                [""] = "",
                ["a"] = new List<object?> { "b", "c" }
            },
            ["stringifyOutput"] = new Dictionary<string, object?>
            {
                ["brackets"] = "=&a[]=b&a[]=c",
                ["indices"] = "=&a[0]=b&a[1]=c",
                ["repeat"] = "=&a=b&a=c"
            },
            ["noEmptyKeys"] = new Dictionary<string, object?>
            {
                ["a"] = new List<object?> { "b", "c" }
            }
        },
        new()
        {
            ["input"] = "[]=a&[]=b& []=1",
            ["withEmptyKeys"] = new Dictionary<string, object?>
            {
                [""] = new List<object?> { "a", "b" },
                [" "] = new List<object?> { "1" }
            },
            ["stringifyOutput"] = new Dictionary<string, object?>
            {
                ["brackets"] = "[]=a&[]=b& []=1",
                ["indices"] = "[0]=a&[1]=b& [0]=1",
                ["repeat"] = "=a&=b& =1"
            },
            ["noEmptyKeys"] = new Dictionary<string, object?>
            {
                ["0"] = "a",
                ["1"] = "b",
                [" "] = new List<object?> { "1" }
            }
        },
        new()
        {
            ["input"] = "[0]=a&[1]=b&a[0]=1&a[1]=2",
            ["withEmptyKeys"] = new Dictionary<string, object?>
            {
                [""] = new List<object?> { "a", "b" },
                ["a"] = new List<object?> { "1", "2" }
            },
            ["noEmptyKeys"] = new Dictionary<string, object?>
            {
                ["0"] = "a",
                ["1"] = "b",
                ["a"] = new List<object?> { "1", "2" }
            },
            ["stringifyOutput"] = new Dictionary<string, object?>
            {
                ["brackets"] = "[]=a&[]=b&a[]=1&a[]=2",
                ["indices"] = "[0]=a&[1]=b&a[0]=1&a[1]=2",
                ["repeat"] = "=a&=b&a=1&a=2"
            }
        },
        new()
        {
            ["input"] = "[deep]=a&[deep]=2",
            ["withEmptyKeys"] = new Dictionary<string, object?>
            {
                [""] = new Dictionary<string, object?>
                {
                    ["deep"] = new List<object?> { "a", "2" }
                }
            },
            ["stringifyOutput"] = new Dictionary<string, object?>
            {
                ["brackets"] = "[deep][]=a&[deep][]=2",
                ["indices"] = "[deep][0]=a&[deep][1]=2",
                ["repeat"] = "[deep]=a&[deep]=2"
            },
            ["noEmptyKeys"] = new Dictionary<string, object?>
            {
                ["deep"] = new List<object?> { "a", "2" }
            }
        },
        new()
        {
            ["input"] = "%5B0%5D=a&%5B1%5D=b",
            ["withEmptyKeys"] = new Dictionary<string, object?>
            {
                [""] = new List<object?> { "a", "b" }
            },
            ["stringifyOutput"] = new Dictionary<string, object?>
            {
                ["brackets"] = "[]=a&[]=b",
                ["indices"] = "[0]=a&[1]=b",
                ["repeat"] = "=a&=b"
            },
            ["noEmptyKeys"] = new Dictionary<string, object?> { ["0"] = "a", ["1"] = "b" }
        }
    ];
}