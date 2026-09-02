import { useTranslation } from 'react-i18next';
import { Card } from '@/shared/ui/Card';
import { AxisBar, type AxisCode } from '@/widgets/AxisBar';
import { PersonalityRadar } from '@/widgets/PersonalityRadar';
import { RiasecChart } from '@/widgets/RiasecChart';
import { ActivityBars } from '@/widgets/ActivityBars';
import type { ActivityResult, BigFiveResult, Mbti16Result, RiasecResult } from '../model/profileTypes';

export interface StudentDiagramsSectionProps {
  mbti16: Mbti16Result | null | undefined;
  bigFive: BigFiveResult | null | undefined;
  riasec: RiasecResult | null | undefined;
  activity: ActivityResult | null | undefined;
}

const AXIS_ORDER: AxisCode[] = ['EI', 'SN', 'TF', 'JP'];

/**
 * Diagrammalar bloki — docs/11 A-5, P25 3-band: "16 tip o'qlari, Big Five radar, RIASEC bar,
 * Aktivlik shkalalari" (P26 widget'laridan foydalanadi). Har blok mustaqil ravishda o'z
 * ma'lumoti bo'lmaganda "natija hali mavjud emas" holatini ko'rsatadi — CLAUDE.md "Bo'sh
 * ma'lumotga chidamlilik".
 */
export function StudentDiagramsSection({
  mbti16,
  bigFive,
  riasec,
  activity,
}: StudentDiagramsSectionProps) {
  const { t } = useTranslation();

  return (
    <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
      <Card title={t('studentProfile.diagrams.axesHeading')}>
        {mbti16 ? (
          <div className="flex flex-col gap-4">
            {AXIS_ORDER.map((axis) => (
              <AxisBar
                key={axis}
                axisCode={axis}
                pct={mbti16.axes[axis].pct}
                letter={mbti16.axes[axis].letter}
                borderline={mbti16.axes[axis].borderline}
              />
            ))}
          </div>
        ) : (
          <p className="text-sm text-neutral-500">{t('studentProfile.diagrams.noData')}</p>
        )}
      </Card>

      <Card title={t('studentProfile.diagrams.radarHeading')}>
        {bigFive ? (
          <PersonalityRadar
            openness={bigFive.factors.O}
            conscientiousness={bigFive.factors.C}
            extraversion={bigFive.factors.E}
            agreeableness={bigFive.factors.A}
            stabilityPct={bigFive.stabilityPct}
          />
        ) : (
          <p className="text-sm text-neutral-500">{t('studentProfile.diagrams.noData')}</p>
        )}
      </Card>

      <Card title={t('studentProfile.diagrams.riasecHeading')}>
        {riasec ? (
          <RiasecChart
            types={riasec.types}
            resultCode={riasec.resultCode}
            differentiation={riasec.differentiation}
          />
        ) : (
          <p className="text-sm text-neutral-500">{t('studentProfile.diagrams.noData')}</p>
        )}
      </Card>

      <Card title={t('studentProfile.diagrams.activityHeading')}>
        {activity ? (
          <ActivityBars
            scales={activity.scales}
            activityIndex={activity.activityIndex}
            activityLevelText={t(`students.enums.activityLevel.${activity.activityLevel}`)}
          />
        ) : (
          <p className="text-sm text-neutral-500">{t('studentProfile.diagrams.noData')}</p>
        )}
      </Card>
    </div>
  );
}
