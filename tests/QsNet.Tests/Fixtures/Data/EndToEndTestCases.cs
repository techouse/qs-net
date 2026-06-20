using System.Collections.Generic;

namespace QsNet.Tests.Fixtures.Data;

public record EndToEndTestCase(Dictionary<string, object?> Data, string Encoded);

internal static class EndToEndTestCases
{
    public static readonly List<EndToEndTestCase> Cases =
    [
        new(new Dictionary<string, object?>(), ""),
        // simple dict with single key-value pair
        new(new Dictionary<string, object?> { { "a", "b" } }, "a=b"),
        // simple dict with multiple key-value pairs 1
        new(new Dictionary<string, object?> { { "a", "b" }, { "c", "d" } }, "a=b&c=d"),
        // simple dict with multiple key-value pairs 2
        new(
            new Dictionary<string, object?>
            {
                { "a", "b" },
                { "c", "d" },
                { "e", "f" }
            },
            "a=b&c=d&e=f"
        ),
        // dict with list
        new(
            new Dictionary<string, object?>
            {
                { "a", "b" },
                { "c", "d" },
                {
                    "e",
                    new List<object?> { "f", "g", "h" }
                }
            },
            "a=b&c=d&e[0]=f&e[1]=g&e[2]=h"
        ),
        // dict with list and nested dict

        new(
            new Dictionary<string, object?>
            {
                { "a", "b" },
                { "c", "d" },
                {
                    "e",
                    new List<object?> { "f", "g", "h" }
                },
                {
                    "i",
                    new Dictionary<string, object?> { { "j", "k" }, { "l", "m" } }
                }
            },
            "a=b&c=d&e[0]=f&e[1]=g&e[2]=h&i[j]=k&i[l]=m"
        ),
        // simple 1-level nested dict

        new(
            new Dictionary<string, object?>
            {
                {
                    "a",
                    new Dictionary<string, object?> { { "b", "c" } }
                }
            },
            "a[b]=c"
        ),
        // two-level nesting

        new(
            new Dictionary<string, object?>
            {
                {
                    "a",
                    new Dictionary<string, object?>
                    {
                        {
                            "b",
                            new Dictionary<string, object?> { { "c", "d" } }
                        }
                    }
                }
            },
            "a[b][c]=d"
        ),
        // list of dicts

        new(
            new Dictionary<string, object?>
            {
                {
                    "a",
                    new List<object?>
                    {
                        new Dictionary<string, object?> { { "b", "c" } },
                        new Dictionary<string, object?> { { "d", "e" } }
                    }
                }
            },
            "a[0][b]=c&a[1][d]=e"
        ),
        // single-item list

        new(
            new Dictionary<string, object?>
            {
                {
                    "a",
                    new List<object?> { "f" }
                }
            },
            "a[0]=f"
        ),
        // nested list inside a dict inside a list

        new(
            new Dictionary<string, object?>
            {
                {
                    "a",
                    new List<object?>
                    {
                        new Dictionary<string, object?>
                        {
                            {
                                "b",
                                new List<object?> { "c" }
                            }
                        }
                    }
                }
            },
            "a[0][b][0]=c"
        ),
        // empty-string value

        new(new Dictionary<string, object?> { { "a", "" } }, "a="),
        // list containing an empty string
        new(
            new Dictionary<string, object?>
            {
                {
                    "a",
                    new List<object?> { "", "b" }
                }
            },
            "a[0]=&a[1]=b"
        ),
        // unicode-only key and value

        new(new Dictionary<string, object?> { { "キー", "値" } }, "キー=値"),
        // emoji (multi-byte unicode) in key and value
        new(new Dictionary<string, object?> { { "🙂", "😊" } }, "🙂=😊"),
        // complex dict with special characters
        new(
            new Dictionary<string, object?>
            {
                {
                    "filters",
                    new Dictionary<string, object?>
                    {
                        {
                            "$or",
                            new List<object?>
                            {
                                new Dictionary<string, object?>
                                {
                                    {
                                        "date",
                                        new Dictionary<string, object?> { { "$eq", "2020-01-01" } }
                                    }
                                },
                                new Dictionary<string, object?>
                                {
                                    {
                                        "date",
                                        new Dictionary<string, object?> { { "$eq", "2020-01-02" } }
                                    }
                                }
                            }
                        },
                        {
                            "author",
                            new Dictionary<string, object?>
                            {
                                {
                                    "name",
                                    new Dictionary<string, object?> { { "$eq", "John Doe" } }
                                }
                            }
                        }
                    }
                }
            },
            "filters[$or][0][date][$eq]=2020-01-01&filters[$or][1][date][$eq]=2020-01-02&filters[author][name][$eq]=John Doe"
        ),
        // dart_api_query/comments_embed_response

        new(
            new Dictionary<string, object?>
            {
                {
                    "commentsEmbedResponse",
                    new List<object?>
                    {
                        new Dictionary<string, object?>
                        {
                            { "id", "1" },
                            { "post_id", "1" },
                            { "someId", "ma018-9ha12" },
                            { "text", "Hello" },
                            {
                                "replies",
                                new List<object?>
                                {
                                    new Dictionary<string, object?>
                                    {
                                        { "id", "3" },
                                        { "comment_id", "1" },
                                        { "someId", "ma020-9ha15" },
                                        { "text", "Hello" }
                                    }
                                }
                            }
                        },
                        new Dictionary<string, object?>
                        {
                            { "id", "2" },
                            { "post_id", "1" },
                            { "someId", "mw012-7ha19" },
                            { "text", "How are you?" },
                            {
                                "replies",
                                new List<object?>
                                {
                                    new Dictionary<string, object?>
                                    {
                                        { "id", "4" },
                                        { "comment_id", "2" },
                                        { "someId", "mw023-9ha18" },
                                        { "text", "Hello" }
                                    },
                                    new Dictionary<string, object?>
                                    {
                                        { "id", "5" },
                                        { "comment_id", "2" },
                                        { "someId", "mw035-0ha22" },
                                        { "text", "Hello" }
                                    }
                                }
                            }
                        }
                    }
                }
            },
            "commentsEmbedResponse[0][id]=1&commentsEmbedResponse[0][post_id]=1&commentsEmbedResponse[0][someId]=ma018-9ha12&commentsEmbedResponse[0][text]=Hello&commentsEmbedResponse[0][replies][0][id]=3&commentsEmbedResponse[0][replies][0][comment_id]=1&commentsEmbedResponse[0][replies][0][someId]=ma020-9ha15&commentsEmbedResponse[0][replies][0][text]=Hello&commentsEmbedResponse[1][id]=2&commentsEmbedResponse[1][post_id]=1&commentsEmbedResponse[1][someId]=mw012-7ha19&commentsEmbedResponse[1][text]=How are you?&commentsEmbedResponse[1][replies][0][id]=4&commentsEmbedResponse[1][replies][0][comment_id]=2&commentsEmbedResponse[1][replies][0][someId]=mw023-9ha18&commentsEmbedResponse[1][replies][0][text]=Hello&commentsEmbedResponse[1][replies][1][id]=5&commentsEmbedResponse[1][replies][1][comment_id]=2&commentsEmbedResponse[1][replies][1][someId]=mw035-0ha22&commentsEmbedResponse[1][replies][1][text]=Hello"
        ),
        // dart_api_query/comments_response

        new(
            new Dictionary<string, object?>
            {
                {
                    "commentsResponse",
                    new List<object?>
                    {
                        new Dictionary<string, object?>
                        {
                            { "id", "1" },
                            { "post_id", "1" },
                            { "someId", "ma018-9ha12" },
                            { "text", "Hello" },
                            {
                                "replies",
                                new List<object?>
                                {
                                    new Dictionary<string, object?>
                                    {
                                        { "id", "3" },
                                        { "comment_id", "1" },
                                        { "someId", "ma020-9ha15" },
                                        { "text", "Hello" }
                                    }
                                }
                            }
                        },
                        new Dictionary<string, object?>
                        {
                            { "id", "2" },
                            { "post_id", "1" },
                            { "someId", "mw012-7ha19" },
                            { "text", "How are you?" },
                            {
                                "replies",
                                new List<object?>
                                {
                                    new Dictionary<string, object?>
                                    {
                                        { "id", "4" },
                                        { "comment_id", "2" },
                                        { "someId", "mw023-9ha18" },
                                        { "text", "Hello" }
                                    },
                                    new Dictionary<string, object?>
                                    {
                                        { "id", "5" },
                                        { "comment_id", "2" },
                                        { "someId", "mw035-0ha22" },
                                        { "text", "Hello" }
                                    }
                                }
                            }
                        }
                    }
                }
            },
            "commentsResponse[0][id]=1&commentsResponse[0][post_id]=1&commentsResponse[0][someId]=ma018-9ha12&commentsResponse[0][text]=Hello&commentsResponse[0][replies][0][id]=3&commentsResponse[0][replies][0][comment_id]=1&commentsResponse[0][replies][0][someId]=ma020-9ha15&commentsResponse[0][replies][0][text]=Hello&commentsResponse[1][id]=2&commentsResponse[1][post_id]=1&commentsResponse[1][someId]=mw012-7ha19&commentsResponse[1][text]=How are you?&commentsResponse[1][replies][0][id]=4&commentsResponse[1][replies][0][comment_id]=2&commentsResponse[1][replies][0][someId]=mw023-9ha18&commentsResponse[1][replies][0][text]=Hello&commentsResponse[1][replies][1][id]=5&commentsResponse[1][replies][1][comment_id]=2&commentsResponse[1][replies][1][someId]=mw035-0ha22&commentsResponse[1][replies][1][text]=Hello"
        ),
        // dart_api_query/post_embed_response

        new(
            new Dictionary<string, object?>
            {
                {
                    "data",
                    new Dictionary<string, object?>
                    {
                        { "id", "1" },
                        { "someId", "af621-4aa41" },
                        { "text", "Lorem Ipsum Dolor" },
                        {
                            "user",
                            new Dictionary<string, object?>
                            {
                                { "firstname", "John" },
                                { "lastname", "Doe" },
                                { "age", "25" }
                            }
                        },
                        {
                            "relationships",
                            new Dictionary<string, object?>
                            {
                                {
                                    "tags",
                                    new Dictionary<string, object?>
                                    {
                                        {
                                            "data",
                                            new List<object?>
                                            {
                                                new Dictionary<string, object?>
                                                {
                                                    { "name", "super" }
                                                },
                                                new Dictionary<string, object?>
                                                {
                                                    { "name", "awesome" }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            },
            "data[id]=1&data[someId]=af621-4aa41&data[text]=Lorem Ipsum Dolor&data[user][firstname]=John&data[user][lastname]=Doe&data[user][age]=25&data[relationships][tags][data][0][name]=super&data[relationships][tags][data][1][name]=awesome"
        ),
        // dart_api_query/post_response

        new(
            new Dictionary<string, object?>
            {
                { "id", "1" },
                { "someId", "af621-4aa41" },
                { "text", "Lorem Ipsum Dolor" },
                {
                    "user",
                    new Dictionary<string, object?>
                    {
                        { "firstname", "John" },
                        { "lastname", "Doe" },
                        { "age", "25" }
                    }
                },
                {
                    "relationships",
                    new Dictionary<string, object?>
                    {
                        {
                            "tags",
                            new List<object?>
                            {
                                new Dictionary<string, object?> { { "name", "super" } },
                                new Dictionary<string, object?> { { "name", "awesome" } }
                            }
                        }
                    }
                }
            },
            "id=1&someId=af621-4aa41&text=Lorem Ipsum Dolor&user[firstname]=John&user[lastname]=Doe&user[age]=25&relationships[tags][0][name]=super&relationships[tags][1][name]=awesome"
        ),
        // dart_api_query/posts_response

        new(
            new Dictionary<string, object?>
            {
                {
                    "postsResponse",
                    new List<object?>
                    {
                        new Dictionary<string, object?>
                        {
                            { "id", "1" },
                            { "someId", "du761-8bc98" },
                            { "text", "Lorem Ipsum Dolor" },
                            {
                                "user",
                                new Dictionary<string, object?>
                                {
                                    { "firstname", "John" },
                                    { "lastname", "Doe" },
                                    { "age", "25" }
                                }
                            },
                            {
                                "relationships",
                                new Dictionary<string, object?>
                                {
                                    {
                                        "tags",
                                        new List<object?>
                                        {
                                            new Dictionary<string, object?> { { "name", "super" } },
                                            new Dictionary<string, object?>
                                            {
                                                { "name", "awesome" }
                                            }
                                        }
                                    }
                                }
                            }
                        },
                        new Dictionary<string, object?>
                        {
                            { "id", "1" },
                            { "someId", "pa813-7jx02" },
                            { "text", "Lorem Ipsum Dolor" },
                            {
                                "user",
                                new Dictionary<string, object?>
                                {
                                    { "firstname", "Mary" },
                                    { "lastname", "Doe" },
                                    { "age", "25" }
                                }
                            },
                            {
                                "relationships",
                                new Dictionary<string, object?>
                                {
                                    {
                                        "tags",
                                        new List<object?>
                                        {
                                            new Dictionary<string, object?> { { "name", "super" } },
                                            new Dictionary<string, object?>
                                            {
                                                { "name", "awesome" }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            },
            "postsResponse[0][id]=1&postsResponse[0][someId]=du761-8bc98&postsResponse[0][text]=Lorem Ipsum Dolor&postsResponse[0][user][firstname]=John&postsResponse[0][user][lastname]=Doe&postsResponse[0][user][age]=25&postsResponse[0][relationships][tags][0][name]=super&postsResponse[0][relationships][tags][1][name]=awesome&postsResponse[1][id]=1&postsResponse[1][someId]=pa813-7jx02&postsResponse[1][text]=Lorem Ipsum Dolor&postsResponse[1][user][firstname]=Mary&postsResponse[1][user][lastname]=Doe&postsResponse[1][user][age]=25&postsResponse[1][relationships][tags][0][name]=super&postsResponse[1][relationships][tags][1][name]=awesome"
        ),
        // dart_api_query/posts_response_paginate

        new(
            new Dictionary<string, object?>
            {
                {
                    "posts",
                    new List<object?>
                    {
                        new Dictionary<string, object?>
                        {
                            { "id", "1" },
                            { "someId", "du761-8bc98" },
                            { "text", "Lorem Ipsum Dolor" },
                            {
                                "user",
                                new Dictionary<string, object?>
                                {
                                    { "firstname", "John" },
                                    { "lastname", "Doe" },
                                    { "age", "25" }
                                }
                            },
                            {
                                "relationships",
                                new Dictionary<string, object?>
                                {
                                    {
                                        "tags",
                                        new List<object?>
                                        {
                                            new Dictionary<string, object?> { { "name", "super" } },
                                            new Dictionary<string, object?>
                                            {
                                                { "name", "awesome" }
                                            }
                                        }
                                    }
                                }
                            }
                        },
                        new Dictionary<string, object?>
                        {
                            { "id", "1" },
                            { "someId", "pa813-7jx02" },
                            { "text", "Lorem Ipsum Dolor" },
                            {
                                "user",
                                new Dictionary<string, object?>
                                {
                                    { "firstname", "Mary" },
                                    { "lastname", "Doe" },
                                    { "age", "25" }
                                }
                            },
                            {
                                "relationships",
                                new Dictionary<string, object?>
                                {
                                    {
                                        "tags",
                                        new List<object?>
                                        {
                                            new Dictionary<string, object?> { { "name", "super" } },
                                            new Dictionary<string, object?>
                                            {
                                                { "name", "awesome" }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                { "total", "2" }
            },
            "posts[0][id]=1&posts[0][someId]=du761-8bc98&posts[0][text]=Lorem Ipsum Dolor&posts[0][user][firstname]=John&posts[0][user][lastname]=Doe&posts[0][user][age]=25&posts[0][relationships][tags][0][name]=super&posts[0][relationships][tags][1][name]=awesome&posts[1][id]=1&posts[1][someId]=pa813-7jx02&posts[1][text]=Lorem Ipsum Dolor&posts[1][user][firstname]=Mary&posts[1][user][lastname]=Doe&posts[1][user][age]=25&posts[1][relationships][tags][0][name]=super&posts[1][relationships][tags][1][name]=awesome&total=2"
        )
    ];
}