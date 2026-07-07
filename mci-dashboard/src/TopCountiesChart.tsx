import * as React from "react";

export type ChartDatum = {
  fipsCode: string;
  name: string;
  value: number;
  rank: number;
  marginOfError: number | null;
};

type TooltipState = {
  datum: ChartDatum;
  x: number;
  y: number;
};

type Props = {
  data: ChartDatum[];
  focusFips: string;
  totalCounties: number;
  format: (value: number) => string;
};

export function TopCountiesChart({ data, focusFips, totalCounties, format }: Props) {
  const [tooltip, setTooltip] = React.useState<TooltipState | null>(null);

  if (data.length === 0) {
    return null;
  }

  const maxValue = Math.max(...data.map((datum) => datum.value), 0) || 1;
  const hasFocus = focusFips !== "";

  return (
    <div className="chart-card">
      <div className="chart-head">
        <h3>Top counties</h3>
        <p>
          Ranked by the selected metric across all {totalCounties} Michigan counties.
          {hasFocus ? " Your focused county is highlighted." : " Hover a bar for detail."}
        </p>
      </div>

      <div className="bar-rows" onMouseLeave={() => setTooltip(null)}>
        {data.map((datum) => {
          const isFocus = datum.fipsCode === focusFips;
          const isContext = hasFocus && !isFocus;
          const widthPercent = Math.max((datum.value / maxValue) * 100, 1.5);

          return (
            <div
              className="bar-row"
              key={datum.fipsCode}
              onMouseEnter={(event) =>
                setTooltip({ datum, x: event.clientX, y: event.clientY })
              }
              onMouseMove={(event) =>
                setTooltip({ datum, x: event.clientX, y: event.clientY })
              }
            >
              <span className="bar-rank">{datum.rank}</span>
              <span className="bar-county" title={datum.name}>
                {datum.name}
              </span>
              <div className="bar-track">
                <div
                  className={isContext ? "bar-fill context" : "bar-fill"}
                  style={{ width: `${widthPercent}%` }}
                />
              </div>
              <span className="bar-value">{format(datum.value)}</span>
            </div>
          );
        })}
      </div>

      {tooltip && (
        <div
          className="chart-tooltip"
          style={{ left: tooltip.x + 14, top: tooltip.y + 14 }}
          role="status"
        >
          <strong>
            {tooltip.datum.name} County · #{tooltip.datum.rank}
          </strong>
          <span>{format(tooltip.datum.value)}</span>
          <span className="chart-tooltip-moe">
            {tooltip.datum.marginOfError === null
              ? "No margin of error"
              : `± ${format(tooltip.datum.marginOfError)} MOE`}
          </span>
        </div>
      )}
    </div>
  );
}
