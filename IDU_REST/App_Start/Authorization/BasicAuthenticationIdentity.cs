﻿using System.Security.Principal;

public class BasicAuthenticationIdentity : GenericIdentity
{
    public BasicAuthenticationIdentity(string name, string password)
        : base(name, "Basic")
    {
        this.Password = password;
    }

    /// <summary>
    /// Basic Auth Password for custom authentication
    /// </summary>
    public string Password { get; set; }
}