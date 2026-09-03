import { stopStack } from './support/stack';

/** Stek va uning bazasi butunlay o'chiriladi (`E2E_KEEP_STACK=1` bilan saqlab qolish mumkin). */
export default async function globalTeardown(): Promise<void> {
  await stopStack();
}
