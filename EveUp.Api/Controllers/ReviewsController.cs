using System.Security.Claims;
using EveUp.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EveUp.Api.Controllers;

[ApiController]
[Route("api/reviews")]
[Authorize]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public ReviewsController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    /// <summary>
    /// Cria uma avaliação para um job
    /// </summary>
    [HttpPost]
    public async Task<ActionResult> Create([FromBody] CreateReviewRequest request)
    {
        var reviewerId = GetUserId();
        var review = await _reviewService.CreateAsync(
            request.JobId, reviewerId, request.ReviewedUserId, request.Rating, request.Comment);
        return Created($"/api/reviews/{review.Id}", new
        {
            review.Id,
            review.JobId,
            review.ReviewerId,
            review.ReviewedUserId,
            review.Rating,
            review.Comment,
            review.CreatedAt
        });
    }

    /// <summary>
    /// Lista avaliações recebidas por um usuário
    /// </summary>
    [HttpGet("user/{userId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult> GetByUser(Guid userId)
    {
        var reviews = await _reviewService.GetByUserAsync(userId);
        return Ok(reviews.Select(r => new
        {
            r.Id,
            r.JobId,
            r.ReviewerId,
            ReviewerName = r.Reviewer?.Name,
            r.Rating,
            r.Comment,
            r.CreatedAt
        }));
    }

    /// <summary>
    /// Lista avaliações de um job
    /// </summary>
    [HttpGet("job/{jobId:guid}")]
    public async Task<ActionResult> GetByJob(Guid jobId)
    {
        var reviews = await _reviewService.GetByJobAsync(jobId);
        return Ok(reviews.Select(r => new
        {
            r.Id,
            r.ReviewerId,
            ReviewerName = r.Reviewer?.Name,
            r.ReviewedUserId,
            ReviewedUserName = r.ReviewedUser?.Name,
            r.Rating,
            r.Comment,
            r.CreatedAt
        }));
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return Guid.Parse(claim?.Value ?? throw new UnauthorizedAccessException());
    }
}

public class CreateReviewRequest
{
    public Guid JobId { get; set; }
    public Guid ReviewedUserId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
}
