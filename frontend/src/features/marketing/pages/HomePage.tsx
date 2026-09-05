import { useTranslation } from 'react-i18next';
import { usePageTitle } from '@/shared/hooks/usePageTitle';
import { HeroSection } from '../sections/HeroSection';
import { HowItWorksSection } from '../sections/HowItWorksSection';
import { MethodBlocksSection } from '../sections/MethodBlocksSection';
import { BenefitsSection } from '../sections/BenefitsSection';
import { FaqSection } from '../sections/FaqSection';
import { CtaBandSection } from '../sections/CtaBandSection';

/**
 * Ommaviy bosh sahifa (`/`) — P45.
 *
 * Auditoriya: maktab rahbarlari, psixologlar va ota-onalar. Bu yerda test BOSHLANMAYDI —
 * o'quvchi testga faqat maktab bergan havola (`/t/:slug`) orqali kiradi, shu sabab barcha
 * CTA'lar aloqa yoki metodika sahifasiga olib boradi.
 *
 * Bo'limlar ketma-ketligi: kim uchun → qanday ishlaydi → nimani o'lchaydi → maktabga nima
 * beradi → savol-javob → yakuniy taklif.
 */
export default function HomePage() {
  const { t } = useTranslation();
  usePageTitle(t('marketing.home.title'));

  return (
    <>
      <HeroSection />
      <HowItWorksSection />
      <MethodBlocksSection />
      <BenefitsSection />
      <FaqSection />
      <CtaBandSection />
    </>
  );
}
