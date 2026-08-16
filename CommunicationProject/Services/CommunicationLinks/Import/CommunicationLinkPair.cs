using CommunicationProject.Models;

namespace CommunicationProject.Services
    .CommunicationLinks.Import;

internal sealed record CommunicationLinkPair(
    CommunicationLink Primary,
    CommunicationLink Reverse);