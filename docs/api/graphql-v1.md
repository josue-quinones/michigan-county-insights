# GraphQL V1

Michigan County Insights exposes a read-only GraphQL surface at `/graphql`
alongside the existing REST endpoints. GraphQL is not a separate data layer:
every resolver calls the same application services (`IReportingQueryService`,
`ICountyInsightService`) that the REST controllers under `/api/reporting` and
`/api/counties` already use. Resolvers do not touch EF Core directly, do not
expose EF entities, and do not recompute rankings, comparisons, or formatted
values that the services already own.

```text
GraphQL Resolver (Mci.Api/GraphQL/ReportingGraphQlQueries.cs)
    |
    v
Existing application/reporting service (IReportingQueryService / ICountyInsightService)
    |
    v
EF Core / SQL Server
```

## Endpoint

- `POST /graphql` — Hot Chocolate GraphQL endpoint (also serves Banana Cake Pop,
  the built-in schema explorer, on `GET /graphql` in the browser).
- REST endpoints and Swagger (`/swagger`) are unchanged and continue to work
  side by side with GraphQL.

## Queries

| Query | Backing service | Notes |
| --- | --- | --- |
| `counties` | `IReportingQueryService.GetCountiesAsync` | All active Michigan counties. |
| `county(fips)` | `IReportingQueryService.GetCountyAsync` | Single county lookup; `null` if unknown. |
| `metrics` | `IReportingQueryService.GetMetricsAsync` | All active metric definitions. |
| `countyDetail(fips, releaseYear)` | `ICountyInsightService.GetCountyDetailAsync` | Full metric snapshot for one county/release. |
| `compareCounties(leftFips, rightFips, releaseYear)` | `ICountyInsightService.CompareCountiesAsync` | Side-by-side comparison; difference math lives in `MetricDifferenceCalculator`, never in the resolver. |
| `rankCounties(metricCode, releaseYear, first)` | `ICountyInsightService.RankCountiesAsync` | Counties ordered by estimate value, descending, limited to `first` (max 50, default 10). |

`releaseYear` defaults to the current default data release when omitted.

## Example queries

### Counties

```graphql
query Counties {
  counties {
    fipsCode
    name
    stateCode
  }
}
```

### County detail

```graphql
query CountyDetail {
  countyDetail(fips: "26161", releaseYear: 2024) {
    county {
      name
      fips
    }
    release {
      displayName
    }
    metrics {
      code
      displayName
      unit
      estimateValue
      marginOfError
      isAvailable
    }
  }
}
```

### Compare counties

```graphql
query CompareCounties {
  compareCounties(leftFips: "26161", rightFips: "26081", releaseYear: 2024) {
    leftCounty {
      name
    }
    rightCounty {
      name
    }
    release {
      displayName
    }
    differenceDirection
    metrics {
      code
      displayName
      unit
      leftValue
      rightValue
      difference
      differenceKind
      percentDifference
    }
  }
}
```

### Rank counties by a metric

```graphql
query RankByIncome {
  rankCounties(metricCode: "median_household_income", releaseYear: 2024, first: 10) {
    rank
    county {
      name
      fips
    }
    estimateValue
    marginOfError
  }
}
```

## Validation

- `fips`, `leftFips`, `rightFips`, and `metricCode` cannot be blank — a blank
  value raises a GraphQL error before any service call.
- Comparing a county with itself raises a GraphQL error
  (`ICountyInsightService.CompareCountiesAsync` rejects it).
- An unknown FIPS code returns `null` for `county`/`countyDetail`, matching the
  existing REST behavior for county detail lookups.
- An unknown county in `compareCounties`, or an unknown `metricCode` in
  `rankCounties`, raises a GraphQL error.
- `first` in `rankCounties` must be between 1 and 50; defaults to 10.

## Current limitations

- GraphQL types are named after their existing C# DTOs (e.g.
  `CountyDetailDto`, `ReportingCountyDto`) rather than the shorter
  `CountyDetail` / `County` names, so the schema stays visibly tied to the
  same contracts REST already returns.
- `rankCounties` is GraphQL-only for now; there is no equivalent REST
  endpoint yet (the dashboard currently ranks client-side from
  `/api/reporting/current-observations`). Adding a REST ranking endpoint is a
  natural follow-up slice, reusing the same `RankCountiesAsync` service
  method.
- No authentication, filtering/sorting middleware, or historical-trend
  queries are included in this slice.
- No mutations are exposed — this is a read-only API surface.
