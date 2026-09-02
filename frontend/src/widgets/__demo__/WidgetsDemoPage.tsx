import { useTranslation } from 'react-i18next';
import { Card } from '@/shared/ui/Card';
import { AxisBar } from '../AxisBar';
import { PersonalityRadar } from '../PersonalityRadar';
import { RiasecChart } from '../RiasecChart';
import { ActivityBars } from '../ActivityBars';
import { IndexGauge } from '../IndexGauge';
import { ReliabilityBadge } from '../ReliabilityBadge';
import { AiReportView, type AiReportSections } from '../AiReportView';

const DEMO_AI_SECTIONS: AiReportSections = {
  summary:
    'Bu — INTJ tipiga xos, tahliliy fikrlaydigan va mustaqil rejalashtiruvchi o\'quvchining namunaviy portreti.',
  personalityPortrait:
    "O'quvchi murakkab masalalarni tizimli tahlil qilishni yaxshi ko'radi, mustaqil ishlashga moyil va uzoq muddatli maqsadlarni aniq ko'ra oladi.",
  strengths: [
    {
      title: 'Tahliliy fikrlash',
      description: "Murakkab ma'lumotni tez tizimlashtira oladi.",
      evidence: 'Big Five: Ochiqlik yuqori, 16 tip: N-qutb ustun',
    },
    {
      title: 'Mustaqillik',
      description: "Tashqi nazoratsiz ham izchil ishlay oladi.",
      evidence: 'Aktivlik: SELF yuqori',
    },
  ],
  growthAreas: [
    {
      title: 'Jamoada ishlash',
      description: "Guruh ishlarida ba'zan chetlanib qolishi mumkin.",
      actionStep: "To'garak yoki loyiha guruhida faol rol olish",
    },
  ],
  learningStyle: "Mustaqil, chuqur o'qish materiallari orqali yaxshi o'zlashtiradi.",
  motivationProfile: "Aniq maqsad va natija ko'rinishi kuchli motivatsiya beradi.",
  activityAssessment: "O'rtacha faol — muntazam mashg'ulotlar tavsiya etiladi.",
  careerSuggestions: [
    {
      field: 'Dasturiy injiniring',
      why: 'Tizimli fikrlash va mustaqil ishlash uslubiga mos',
      exampleProfessions: ['Backend dasturchi', "Ma'lumotlar tahlilchisi"],
      nextSteps: ['Algoritmlar kursi', 'Kichik loyiha qilib ko\'rish'],
    },
  ],
  studentRecommendations: ["Har hafta bitta kichik loyiha ustida ishlab ko'r."],
  teacherNotes: ['Mustaqil topshiriqlarda yaxshi natija beradi.'],
  parentNotes: ["Uyda mustaqil ishlash uchun tinch joy ajratib bering."],
  attentionFlags: [],
  disclaimer: "Bu tahlil tashxis emas — hozirgi holat surati, vaqt o'tishi bilan o'zgarishi mumkin.",
};

/**
 * Barcha diagramma widget'lari namunaviy ma'lumot bilan — `/admin/_widgets` (faqat dev,
 * `import.meta.env.DEV` — `app/router.tsx`). P26 DoD: "Barcha 7 widget yozildi va demo
 * sahifada ko'rinadi". Storybook shart emas (P26 cheklovi).
 */
export default function WidgetsDemoPage() {
  const { t } = useTranslation();

  return (
    <div className="flex flex-col gap-6 pb-12">
      <div>
        <h1 className="text-xl font-semibold text-neutral-900">{t('widgetsDemo.title')}</h1>
        <p className="text-sm text-neutral-500">{t('widgetsDemo.description')}</p>
      </div>

      <Card title="AxisBar — 0%, 50% (muvozanat), 100%">
        <div className="flex flex-col gap-4">
          <AxisBar axisCode="EI" pct={0} letter="I" borderline={false} />
          <AxisBar axisCode="SN" pct={50} letter="S" borderline />
          <AxisBar axisCode="TF" pct={33.3} letter="T" borderline={false} />
          <AxisBar axisCode="JP" pct={100} letter="J" borderline={false} />
        </div>
      </Card>

      <Card title="PersonalityRadar">
        <PersonalityRadar
          openness={{ pct: 70, level: 'Yuqori' }}
          conscientiousness={{ pct: 77.5, level: 'Yuqori' }}
          extraversion={{ pct: 35, level: 'Past' }}
          agreeableness={{ pct: 62.5, level: 'Yuqori' }}
          stabilityPct={70}
        />
      </Card>

      <Card title="RiasecChart">
        <RiasecChart
          types={{ R: 62, I: 88, ART: 71, SOC: 40, ENT: 35, CONV: 48 }}
          resultCode="IRA"
          differentiation={53}
        />
      </Card>

      <Card title="ActivityBars">
        <ActivityBars
          scales={{ MOT: 74, SELF: 68, SOCA: 52, ENG: 60 }}
          activityIndex={65.2}
          activityLevelText="O'rtacha faol"
        />
      </Card>

      <Card title="IndexGauge — 0, 50, 100">
        <div className="grid grid-cols-1 gap-6 sm:grid-cols-3">
          <IndexGauge value={0} label="Yetuklik indeksi" levelText="Shakllanish bosqichida" />
          <IndexGauge value={50} label="Yetuklik indeksi" levelText="O'rtacha" />
          <IndexGauge value={100} label="Yetuklik indeksi" levelText="Yuqori" />
        </div>
      </Card>

      <Card title="ReliabilityBadge — 3 holat">
        <div className="flex flex-wrap gap-3">
          <ReliabilityBadge flag="Reliable" score={82.5} />
          <ReliabilityBadge flag="Questionable" score={55} />
          <ReliabilityBadge flag="Unreliable" score={20} />
        </div>
      </Card>

      <Card title="AiReportView — to'liq ma'lumot">
        <AiReportView sections={DEMO_AI_SECTIONS} />
      </Card>

      <Card title="AiReportView — bo'sh ma'lumot (chidamlilik)">
        <AiReportView sections={{}} />
      </Card>
    </div>
  );
}
