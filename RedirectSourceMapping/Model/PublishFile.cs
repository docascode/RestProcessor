namespace RedirectSourceMapping.Model
{
    using System;

    public class PublishFile
    {
        [Field(Name ="File Type")]
        public string FileType { get; set; }
        [Field(Name = "Content Git Url")]
        public string ContentGitUrl { get; set; }
        [Field(Name = "Publish Url")]
        public string PublishUrl { get; set; }
        [Field(Name = "Publish State")]
        public string PublishState { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class FieldAttribute : Attribute
    {
        public string Name { get; set; }
        public string Index { get; set; }
    }
}
